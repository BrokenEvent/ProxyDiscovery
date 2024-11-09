using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Helpers;
using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Checkers
{
  /// <summary>
  /// Checks proxy availability by connecting with the proxy to given URL. Supports only HTTPS proxies (requires proxy to support CONNECT
  /// HTTP method).
  /// </summary>
  public sealed class ProxyHttpConnectChecker: IProxyChecker
  {
    private IProxyTunnelTester actualTunnelTester;

    private byte[] requestBytes;

    /// <summary>
    /// Gets or sets the target URL to check.
    /// </summary>
    /// <remarks>There will be many requests to that server, so please use server which you don't mind.</remarks>
    public Uri TargetUrl { get; set; }

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

    public IProxyTunnelTester TunnelTester { get; set; } = new HttpHeadTunnelTester();

    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      if (TargetUrl == null)
        yield return "Target URL cannot be empty";

      if (TunnelTester == null)
        yield return "Tunnel tester must not be null.";
    }

    /// <inheritdoc />
    public void Prepare()
    {
      requestBytes = HttpRequestBuilder.BuildRequest(HttpVersion, "CONNECT", TargetUrl.Host, TargetUrl.Port);

      switch (TunnelTester.Protocol)
      {
        case TunnelTesterProtocol.Http:
          if (TargetUrl.Scheme == "https")
            // wrap with SSL tester
            actualTunnelTester = new SslTunnelTester(TunnelTester);
          else
            // otherwise leave it as it is
            goto default;
          break;

        case TunnelTesterProtocol.SSL:
          // check if we can use it
          if (TargetUrl.Scheme == "http")
            throw new InvalidOperationException($"Unable to use proxy tunnel tester. {TunnelTester.GetType().Name} uses SSL, but the target URL uses http.");
          // otherwise leave it as it is
          goto default;

        default:
          actualTunnelTester = TunnelTester;
          break;
      }
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
        int received = await stream.ReadAsync(buffer, 0, buffer.Length, ct);

        sw.Stop();

        // 0 indicates the stream is closed
        if (received == 0)
          return new ProxyState(proxy, ProxyCheckResult.ServiceRefused, "Connection closed by the proxy server.", sw.Elapsed);

        // parse the response
        HttpResponseParser response = new HttpResponseParser(buffer, received);

        // is response valid?
        if (!response.IsValid)
          return new ProxyState(proxy, ProxyCheckResult.UnparsableResponse, "Couldn't parse proxy response.", sw.Elapsed);

        // if the status OK? all 2xx codes are counted as OK.
        if (response.StatusCode < 200 || response.StatusCode >= 300)
          return new ProxyState(proxy, ProxyCheckResult.ServiceRefused, $"Proxy status: {response.StatusCode} {response.Phrase}", sw.Elapsed);

        // now we're certain that the proxy supports HTTPS
        proxy.IsHttps = true;

        // test the created connection
        TunnelTestResult tunnelTestResult = await actualTunnelTester.CheckTunnel(TargetUrl, stream, ct);

        // and can return the test result
        return new ProxyState(proxy, tunnelTestResult.Result, tunnelTestResult.Message, sw.Elapsed);
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
  }
}
