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
  /// Connects to SOCKS5 proxy.
  /// </summary>
  public class Socks5Checker: IProxyProtocolChecker
  {
    private byte[] methodsRequestBytes;
    private byte[] connectRequestBytes;

    public void Prepare(Uri targetUrl)
    {
      // generate METHODS request. This checker supports only non-authorized proxies, so only one method
      methodsRequestBytes = Socks5Parser.BuildMethodRequest(Socks5Method.None);

      // generate CONNECT request
      connectRequestBytes = Socks5Parser.BuildRequest(Socks5Command.Connect, targetUrl.Host, (ushort)targetUrl.Port);
    }

    public async Task<TestResult> TestConnection(Uri targetUrl, ProxyInformation proxy, NetworkStream stream, CancellationToken ct)
    {
      // send the METHODS request
      await stream.WriteAsync(methodsRequestBytes, 0, methodsRequestBytes.Length, ct);

      // respect the cancellation token
      if (ct.IsCancellationRequested)
        return new TestResult(ProxyCheckResult.Canceled, "Check has been canceled");

      byte[] responseBytes = new byte[1024];

      // wait for the answer
      int received = await stream.ReadAsync(responseBytes, 0, responseBytes.Length, ct);

      // 0 indicates the stream is closed
      if (received == 0)
        return new TestResult(ProxyCheckResult.ServiceRefused, "Connection closed by the proxy server.");

      // parse the response to METHODS request
      Socks5Method methodResponse = Socks5Parser.ParseMethodResponse(responseBytes);

      // does the proxy allow unauthorized access?
      if (methodResponse == Socks5Method.NoAcceptableMethods)
        return new TestResult(ProxyCheckResult.ServiceRefused, "Socks5 proxy does not support non-authorized connectes.");

      // respect the cancellation token
      if (ct.IsCancellationRequested)
        return new TestResult(ProxyCheckResult.Canceled, "Check has been canceled");

      // now send CONNECT request
      await stream.WriteAsync(connectRequestBytes, 0, connectRequestBytes.Length, ct);

      // respect the cancellation token
      if (ct.IsCancellationRequested)
        return new TestResult(ProxyCheckResult.Canceled, "Check has been canceled");

      // wait for the answer
      received = await stream.ReadAsync(responseBytes, 0, responseBytes.Length, ct);

      // 0 indicates the stream is closed
      if (received == 0)
        return new TestResult(ProxyCheckResult.ServiceRefused, "Connection closed by the proxy server.");

      // parse the response to CONNECT request 
      Socks5Parser connectResponse = new Socks5Parser(responseBytes);

      // check the response status
      if (connectResponse.Reply != Socks5Reply.OK)
        return new TestResult(ProxyCheckResult.ServiceRefused, $"Proxy response: {connectResponse.Reply} ({(byte)connectResponse.Reply:X2})");

      // as being a stream-oriented service, socks5 must support SSL
      proxy.IsSSL = true;

      return new TestResult(ProxyCheckResult.OK, "Connected");
    }

    public IEnumerable<string> Validate()
    {
      yield break;
    }
  }
}
