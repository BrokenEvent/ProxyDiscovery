using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Helpers;
using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Checkers
{
  /// <summary>
  /// Connects to SOCKS4 proxy.
  /// </summary>
  /// <remarks>Will work with SOCKS4A using the SOCKS4 scheme.</remarks>
  public class Socks4Checker: IProxyProtocolChecker
  {
    private byte[] requestBytes;

    /// <summary>
    /// Gets or sets the value to pass as Socks4 userid.
    /// </summary>
    public string Socks4UserId { get; set; } = "ProxyDiscovery";

    public void Prepare(Uri targetUrl)
    {
      // socks4 works with IP addresses, so we should resolve it before
      IPHostEntry ipHostEntry = Dns.GetHostEntry(targetUrl.Host);

      // prepare the connect request
      requestBytes = Socks4Builder.BuildConnectRequest(ipHostEntry.AddressList[0], (ushort)targetUrl.Port, Socks4UserId);
    }

    public async Task<TestResult> TestConnection(Uri targetUrl, ProxyInformation proxy, NetworkStream stream, CancellationToken ct)
    {
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
      Socks4Result response = Socks4Builder.ParseConnectResponse(responseBytes);

      // check the response status
      if (response != Socks4Result.OK)
        return new TestResult(ProxyCheckResult.ServiceRefused, $"Proxy response: {response} ({(byte)response:X2})");

      // as being a stream-oriented service, socks4 must support SSL
      proxy.IsSSL = true;

      return new TestResult(ProxyCheckResult.OK, "Connected");
    }

    public IEnumerable<string> Validate()
    {
      yield break;
    }
  }
}
