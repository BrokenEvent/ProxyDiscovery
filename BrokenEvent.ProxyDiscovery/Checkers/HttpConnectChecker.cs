using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Helpers;
using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Checkers
{
  /// <summary>
  /// Connects to HTTP proxy.
  /// </summary>
  public class HttpConnectChecker: IProxyProtocolChecker
  {
    private byte[] requestBytes;

    /// <summary>
    /// Gets or sets the HTTP version to check with.
    /// </summary>
    public HttpVersion HttpVersion { get; set; } = HttpVersion.OneOne;

    public void Prepare(Uri targetUrl)
    {
      requestBytes = HttpRequestBuilder.BuildRequest(HttpVersion, "CONNECT", targetUrl.Host, targetUrl.Port);
    }

    public async Task<TestResult> TestConnection(Uri targetUrl, ProxyInformation proxy, NetworkStream stream, CancellationToken ct)
    {
      if (proxy.IsSSL.HasValue && !proxy.IsSSL.Value)
        return new TestResult(ProxyCheckResult.Unchecked, "HttpConnectChecker doesn't support non-HTTPS proxies");

      // send the CONNECT request
      await stream.WriteAsync(requestBytes, 0, requestBytes.Length, ct);

      // respect the cancellation token
      if (ct.IsCancellationRequested)
        return new TestResult(ProxyCheckResult.Canceled, "Check has been canceled");

      byte[] responseBytes = new byte[1024];

      // wait for the answer
      int received = await stream.ReadAsync(responseBytes, 0, responseBytes.Length, ct);

      // 0 indicates the stream is closed
      if (received == 0)
        return new TestResult(ProxyCheckResult.ServiceRefused, "Connection closed by the proxy server.");

      // parse the response
      HttpResponseParser response = new HttpResponseParser(responseBytes, received);

      // is response valid?
      if (!response.IsValid)
        return new TestResult(ProxyCheckResult.UnparsableResponse, "Couldn't parse proxy response.");

      // if the status OK? all 2xx codes are counted as OK.
      if (response.StatusCode < 200 || response.StatusCode >= 300)
        return new TestResult(ProxyCheckResult.ServiceRefused, $"Proxy status: {response.StatusCode} {response.Phrase}");

      // now we're certain that the proxy supports SSL
      proxy.IsSSL = true;
      return new TestResult(ProxyCheckResult.OK, response.Phrase);
    }

    public IEnumerable<string> Validate()
    {
      yield break;
    }
  }
}
