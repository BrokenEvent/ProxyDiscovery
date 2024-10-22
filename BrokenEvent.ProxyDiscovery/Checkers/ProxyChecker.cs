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

    private string BuildConnectRequest(bool appendHost = true)
    {
      Uri uri = new Uri(TargetUrl);

      StringBuilder sb = new StringBuilder();
      sb.Append("CONNECT ").Append(uri.Host).Append(':').Append(uri.Port).Append(" HTTP/1.1\r\n");

      if (appendHost)
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

      TcpClient client = new TcpClient();
      client.LingerState.Enabled = false;
      client.NoDelay = true;
      client.SendTimeout = Timeout;
      client.ReceiveTimeout = Timeout;

      Stopwatch sw = Stopwatch.StartNew();
      try
      {
        using (ct.Register(client.Close))
        {
          await client.ConnectAsync(proxy.Address, proxy.Port).ConfigureAwait(false);
        }

        NetworkStream stream = client.GetStream();

        await stream.WriteAsync(requestBytes, 0, requestBytes.Length, ct).ConfigureAwait(false);

        byte[] buffer = new byte[1024];

        int recv = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);

        sw.Stop();

        if (recv == 0)
          return new ProxyState(proxy, ProxyCheckResult.ServiceRefused, "Connection closed by the proxy server.", sw.Elapsed);

        ProxyResponse response = new ProxyResponse(Encoding.ASCII.GetString(buffer, 0, recv));

        if (!response.IsValid)
          return new ProxyState(proxy, ProxyCheckResult.UnparsableResponse, "Couldn't parse proxy response.", sw.Elapsed);

        if (response.StatusCode != 200)
          return new ProxyState(proxy, ProxyCheckResult.ServiceRefused, $"Proxy status: {response.StatusCode} {response.Phrase}", sw.Elapsed);

        // now we're certain that the proxy supports HTTPS
        proxy.IsHttps = true;

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
      catch (Exception e)
      {
        return new ProxyState(proxy, ProxyCheckResult.Failure, e.Message, TimeSpan.Zero);
      }
      finally
      {
        //sw.Stop();
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
}
