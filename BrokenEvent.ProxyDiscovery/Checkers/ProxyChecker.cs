using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Helpers;
using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Checkers
{
  /// <summary>
  /// Checks proxy availability by connecting with the proxy to given URL. Supports only HTTPS proxies.
  /// </summary>
  public sealed class ProxyChecker: IProxyChecker
  {
    private byte[] requestBytes;

    /// <summary>
    /// Gets or sets the target URL to check.
    /// </summary>
    /// <remarks>There will be many requests to that server, so please use server which you don't mind.</remarks>
    public string TargetUrl { get; set; }

    /// <summary>
    /// Gets or sets the send/receive timeout in milliseconds. The default is 0.
    /// </summary>
    public int Timeout { get; set; }

    /// <summary>
    /// Gets or sets the HTTP version to check with.
    /// </summary>
    public HttpVersion HttpVersion { get; set; } = HttpVersion.OneOne;

    /// <summary>
    /// Gets or sets the value indicating whether to cancel gracefully.
    /// </summary>
    /// <remarks>
    /// <para>Non-graceful operation cancel will stop all connections immediately (<see cref="TcpClient.Close"/>) which may lead to different internal
    /// exceptions up to NPE and a possible leak of socket resources.</para>
    /// <para>Graceful operation cancel will wait until the current async operation (socket connect/send/receive) is finished.
    /// In worst cases this may take up to several tens of seconds to completely cancel the proxy discovery.</para>
    /// </remarks>
    public bool GracefulCancel { get; set; } = true;

    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      if (string.IsNullOrWhiteSpace(TargetUrl))
        yield return "Target URL cannot be empty";
    }

    /// <inheritdoc />
    public void Prepare()
    {
      if (TargetUrl == null)
        throw new InvalidOperationException("Unable to prepare proxy checker, no target URL specified.");

      requestBytes = Encoding.ASCII.GetBytes(BuildConnectRequest());
    }

    private string BuildConnectRequest()
    {
      Uri uri = new Uri(TargetUrl);

      StringBuilder sb = new StringBuilder();
      sb.Append("CONNECT ").Append(uri.Host).Append(':').Append(uri.Port).Append(" HTTP/");

      switch (HttpVersion)
      {
        case HttpVersion.OneZero:
          sb.Append("1.0");
          break;

        case HttpVersion.OneOne:
          sb.Append("1.0");
          break;

        default:
          throw new InvalidOperationException($"Invalid or unsupported HTTP version: {HttpVersion}");
      }

      sb.Append("\r\n");

      if (HttpVersion == HttpVersion.OneOne)
        sb.Append("Host: ").Append(uri.Host).Append(':').Append(uri.Port).Append("\r\n");

      sb.Append("\r\n");

      return sb.ToString();
    }

    /// <inheritdoc />
    public async Task<ProxyState> CheckProxy(ProxyInformation proxy, CancellationToken ct)
    {
      if (requestBytes == null)
        throw new InvalidOperationException("Proxy checker not initialized, consider Prepare() call.");

      if (proxy.IsHttps.HasValue && !proxy.IsHttps.Value)
        return new ProxyState(proxy, ProxyCheckResult.Unckeched, "ProxyChecker doesn't support non-HTTPS proxies", TimeSpan.Zero);

      // respect the cancellation token
      if (ct.IsCancellationRequested)
        return new ProxyState(proxy, ProxyCheckResult.Canceled, "Check has been canceled", TimeSpan.Zero);

      TcpClient client = new TcpClient();
      client.LingerState.Enabled = false;
      client.NoDelay = true;
      client.SendTimeout = Timeout;
      client.ReceiveTimeout = Timeout;

      Stopwatch sw = Stopwatch.StartNew();

      // if we intent to cancel non-gracefully, register TcpClient.Close() to be called once the cancel is received. Only if.
      CancellationTokenRegistration? ctr = null;
      if (!GracefulCancel)
        ctr = ct.Register(client.Close);

      try
      {
        // attempt to connect to target host
        await client.ConnectAsync(proxy.Address, proxy.Port).ConfigureAwait(false);

        // respect the cancellation token
        if (ct.IsCancellationRequested)
          return new ProxyState(proxy, ProxyCheckResult.Canceled, "Check has been canceled", TimeSpan.Zero);

        NetworkStream stream = client.GetStream();

        // send the CONNECT request
        await stream.WriteAsync(requestBytes, 0, requestBytes.Length, ct);

        // respect the cancellation token
        if (ct.IsCancellationRequested)
          return new ProxyState(proxy, ProxyCheckResult.Canceled, "Check has been canceled", TimeSpan.Zero);

        byte[] buffer = new byte[1024];

        // wait for the answer
        int recv = await stream.ReadAsync(buffer, 0, buffer.Length, ct);

        sw.Stop();

        // 0 indicates the stream is closed
        if (recv == 0)
          return new ProxyState(proxy, ProxyCheckResult.ServiceRefused, "Connection closed by the proxy server.", sw.Elapsed);

        // parse the response
        ProxyResponse response = new ProxyResponse(Encoding.ASCII.GetString(buffer, 0, recv));

        // is response valid?
        if (!response.IsValid)
          return new ProxyState(proxy, ProxyCheckResult.UnparsableResponse, "Couldn't parse proxy response.", sw.Elapsed);

        // if the status OK? all 2xx codes are counted as OK.
        if (response.StatusCode < 200 || response.StatusCode >= 300)
          return new ProxyState(proxy, ProxyCheckResult.ServiceRefused, $"Proxy status: {response.StatusCode} {response.Phrase}", sw.Elapsed);

        // now we're certain that the proxy supports HTTPS
        proxy.IsHttps = true;

        // and can return success
        return new ProxyState(proxy, ProxyCheckResult.OK, response.Phrase, sw.Elapsed);
      }
      catch (SocketException e)
      {
        return new ProxyState(proxy, ProxyCheckResult.NetworkError, e.Message, sw.Elapsed);
      }
      catch (IOException e)
      {
        return new ProxyState(proxy, ProxyCheckResult.NetworkError, e.Message, sw.Elapsed);
      }
      catch (OperationCanceledException)
      {
        return new ProxyState(proxy, ProxyCheckResult.Canceled, "Check has been canceled", TimeSpan.Zero);
      }
      catch (Exception e)
      {
        // we can encounter different types of exceptions on cancel, even NPEs, so don't log them.
        if (ct.IsCancellationRequested)
          return new ProxyState(proxy, ProxyCheckResult.Canceled, "Check has been canceled", TimeSpan.Zero);

        return new ProxyState(proxy, ProxyCheckResult.Failure, e.Message, TimeSpan.Zero);
      }
      finally
      {
        ctr?.Dispose();
        client.Dispose();
      }
    }

    private struct ProxyResponse
    {
      public readonly string HttpVersion;
      public readonly int StatusCode;
      public readonly string Phrase;
      public readonly bool IsValid;

      public ProxyResponse(string response)
        : this()
      {
        // HTTP/1.0 200 Connection established\r\n\r\n

        if (!response.StartsWith("HTTP/"))
          return;

        int i = 5;

        HttpVersion = StringHelpers.ReadUntil(response, ref i, " ");
        if (HttpVersion == null)
          return;

        string code = StringHelpers.ReadUntil(response, ref i, " ");
        if (code == null || !int.TryParse(code, out StatusCode))
          return;

        Phrase = StringHelpers.ReadUntil(response, ref i, "\r\n");
        IsValid = true;
      }
    }
  }
 
  public enum HttpVersion
  {
    /// <summary>
    /// Test on HTTP/1.0
    /// </summary>
    OneZero,
    /// <summary>
    /// Test on HTTP/1.1
    /// </summary>
    /// <remarks>The only difference is that Host header is required for 1.1.</remarks>
    OneOne
  }
}
