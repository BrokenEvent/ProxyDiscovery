using System;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Checkers;
using BrokenEvent.ProxyDiscovery.Interfaces;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class ProxyHttpConnectCheckerTests
  {
    public class U
    {
      public int Port { get; }
      public TestServer Server { get; }
      public IProxyTunnelTester TunnelTester { get; }
      public HttpVersion Version { get; }
      public ProxyCheckResult ExpectedResult { get; }

      public U(int port, TestServer server, IProxyTunnelTester tunnelTester, HttpVersion version, ProxyCheckResult expectedResult)
      {
        Port = port;
        Server = server;
        TunnelTester = tunnelTester;
        Version = version;
        ExpectedResult = expectedResult;
      }

      public override string ToString()
      {
        return $"{Port} {Version} {TunnelTester.GetType().Name} Server: '{Server}' Expected: {ExpectedResult}";
      }
    }

    private static int portCounter = 20001;

    public static readonly U[] testData = new U[]
    {
      // HTTP/1.0 via none
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.0\r\n\r\n", "HTTP/1.0 200 OK\r\n\r\n"),
          new NoneTunnelTester(),
          HttpVersion.OneZero,
          ProxyCheckResult.OK
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.0\r\n\r\n", "HTTP/1.0 403 Forbidden\r\n\r\n"),
          new NoneTunnelTester(),
          HttpVersion.OneZero,
          ProxyCheckResult.ServiceRefused
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.0\r\n\r\n", "HTTP/1.0 502 Bad Gateway\r\n\r\n"),
          new NoneTunnelTester(),
          HttpVersion.OneZero,
          ProxyCheckResult.ServiceRefused
        ),

      // HTTP1.1 via none
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n"),
          new NoneTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.OK
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.0\r\n\r\n", "HTTP/1.1 502 Bad Gateway\r\n\r\n"),
          new NoneTunnelTester(),
          HttpVersion.OneZero,
          ProxyCheckResult.ServiceRefused
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.0\r\n\r\n", "HTTP/1.1 403 Forbidden\r\n\r\n"),
          new NoneTunnelTester(),
          HttpVersion.OneZero,
          ProxyCheckResult.ServiceRefused
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "oouoiuouoiui"),
          new NoneTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.UnparsableResponse
        ),
      new U(
          portCounter++,
          null,
          new NoneTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.NetworkError
        ),

      // HTTP1.1 via head
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange("HEAD / HTTP/1.1\r\nHost:test.com\r\n\r\n", "HTTP/1.0 200 OK\r\n\r\n"),
          new HttpHeadTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.OK
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange("HEAD / HTTP/1.1\r\nHost:test.com\r\n\r\n", "HTTP/1.1 404 Not Found\r\n\r\n"),
          new HttpHeadTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.OK
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange("HEAD / HTTP/1.1\r\nHost:test.com\r\n\r\n", "uihuiuhiuhiuh"),
          new HttpHeadTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.UnparsableResponse
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n"),
          new HttpHeadTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.NetworkError
        ),

      // HTTP1.1 via trace
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange("TRACE / HTTP/1.1\r\nHost:test.com\r\n\r\n", "HTTP/1.0 200 OK\r\n\r\n"),
          new HttpTraceTunnelTester(), 
          HttpVersion.OneOne,
          ProxyCheckResult.OK
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange("TRACE / HTTP/1.1\r\nHost:test.com\r\n\r\n", "HTTP/1.1 405 Method Not Allowed\r\n\r\n"),
          new HttpTraceTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.OK
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange("TRACE / HTTP/1.1\r\nHost:test.com\r\n\r\n", "uihuiuhiuhiuh"),
          new HttpTraceTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.UnparsableResponse
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n"),
          new HttpTraceTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.NetworkError
        ),
    };

    [TestCaseSource(nameof(testData))]
    public async Task Test(U u)
    {
#pragma warning disable 4014
      if (u.Server != null)
        Task.Run(() => u.Server.Start(u.Port));
#pragma warning restore 4014

      ProxyHttpConnectChecker checker = new ProxyHttpConnectChecker
      {
        TargetUrl = new Uri("http://test.com"),
        TunnelTester = u.TunnelTester,
        HttpVersion = u.Version
      };

      checker.Prepare();
      ProxyState state = await checker.CheckProxy(new ProxyInformation("127.0.0.1", (ushort)u.Port), CancellationToken.None);

      if (u.Server != null)
        Assert.IsNull(u.Server.Error);
      Assert.AreEqual(u.ExpectedResult, state.Result);
    }
  }
}
