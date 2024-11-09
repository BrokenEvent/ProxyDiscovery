using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Helpers;
using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Checkers
{
  /// <summary>
  /// Tests the TCP tunnel via HTTP HEAD request.
  /// </summary>
  /// <remarks>Even if the server returns HTTP error, it still means the connection is operational.</remarks>
  public class HttpHeadTunnelTester: IProxyTunnelTester
  {
    public async Task<TunnelTestResult> CheckTunnel(Uri uri, Stream stream, CancellationToken ct)
    {
      // build request
      byte[] request = HttpRequestBuilder.BuildRequest(
          HttpVersion.OneOne,
          "HEAD",
          uri.Host,
          uri.IsDefaultPort ? (int?)null : uri.Port,
          uri.LocalPath
        );

      // send it
      await stream.WriteAsync(request, 0, request.Length, ct);

      // now it's time to check whether there is a cancel request
      if (ct.IsCancellationRequested)
        return new TunnelTestResult(ProxyCheckResult.Canceled, "Tunnel check has been canceled");

      // read response
      byte[] buffer = new byte[1000];
      int received = await stream.ReadAsync(buffer, 0, buffer.Length, ct);

      if (received == 0)
        return new TunnelTestResult(ProxyCheckResult.ServiceRefused, "Connection closed during target response receiving.");

      // parse it
      HttpResponseParser response = new HttpResponseParser(buffer, received);

      // is valid/parsable?
      if (!response.IsValid)
        return new TunnelTestResult(ProxyCheckResult.UnparsableResponse, "Couldn't parse target server's response.");

      // error HTTP response is counted as ok because it still means the connection is operational
      if (response.StatusCode >= 400)
        return new TunnelTestResult(ProxyCheckResult.OK, $"Target server responds with error: {response.StatusCode} {response.Phrase}");

      // well' nothing more to check, return success
      return new TunnelTestResult(ProxyCheckResult.OK, response.Phrase);
    }

    public TunnelTesterProtocol Protocol
    {
      get { return TunnelTesterProtocol.Http; }
    }
  }
}
