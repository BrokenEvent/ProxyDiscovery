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
      public string Name { get; }
      public string Target { get; }

      public U(int port, TestServer server, IProxyTunnelTester tunnelTester, HttpVersion version, ProxyCheckResult expectedResult, string name, string target = "http://test.com")
      {
        Port = port;
        Server = server;
        TunnelTester = tunnelTester;
        Version = version;
        ExpectedResult = expectedResult;
        Name = name;
        Target = target;
      }

      public override string ToString()
      {
        return $"{Name}. Expected: {ExpectedResult}";
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
          ProxyCheckResult.OK,
          "HTTP/1.0 - None - 200"
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.0\r\n\r\n", "HTTP/1.0 403 Forbidden\r\n\r\n"),
          new NoneTunnelTester(),
          HttpVersion.OneZero,
          ProxyCheckResult.ServiceRefused,
          "HTTP/1.0 - None - 403"
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.0\r\n\r\n", "HTTP/1.0 502 Bad Gateway\r\n\r\n"),
          new NoneTunnelTester(),
          HttpVersion.OneZero,
          ProxyCheckResult.ServiceRefused,
          "HTTP/1.0 - None - 502"
        ),

      // HTTP1.1 via none
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n"),
          new NoneTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.OK,
          "HTTP/1.1 - None - 200"
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.0\r\n\r\n", "HTTP/1.1 502 Bad Gateway\r\n\r\n"),
          new NoneTunnelTester(),
          HttpVersion.OneZero,
          ProxyCheckResult.ServiceRefused,
          "HTTP/1.1 - None - 502"
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.0\r\n\r\n", "HTTP/1.1 403 Forbidden\r\n\r\n"),
          new NoneTunnelTester(),
          HttpVersion.OneZero,
          ProxyCheckResult.ServiceRefused,
          "HTTP/1.1 - None - 403"
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "oouoiuouoiui"),
          new NoneTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.UnparsableResponse,
          "HTTP/1.1 - None - Unparsable"
        ),
      new U(
          portCounter++,
          null,
          new NoneTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.NetworkError,
          "HTTP/1.1 - None - No server"
        ),

      // HTTP1.1 via head
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange("HEAD / HTTP/1.1\r\nHost:test.com\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n"),
          new HttpHeadTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.OK,
          "HTTP/1.1 - Head - 200"
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange("HEAD / HTTP/1.1\r\nHost:test.com\r\n\r\n", "HTTP/1.1 404 Not Found\r\n\r\n"),
          new HttpHeadTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.OK,
          "HTTP/1.1 - Head - 200 - 404"
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange("HEAD / HTTP/1.1\r\nHost:test.com\r\n\r\n", "uihuiuhiuhiuh"),
          new HttpHeadTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.UnparsableResponse,
          "HTTP/1.1 - Head - 200 - Unparsable"
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n"),
          new HttpHeadTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.NetworkError,
          "HTTP/1.1 - Head - 200 - None"
        ),

      // HTTP1.1 via trace
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange("TRACE / HTTP/1.1\r\nHost:test.com\r\n\r\n", "HTTP/1.0 200 OK\r\n\r\n"),
          new HttpTraceTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.OK,
          "HTTP/1.1 - Trace - 200 - 200"
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange("TRACE / HTTP/1.1\r\nHost:test.com\r\n\r\n", "HTTP/1.1 405 Method Not Allowed\r\n\r\n"),
          new HttpTraceTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.OK,
          "HTTP/1.1 - Trace - 200 - 405"
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange("TRACE / HTTP/1.1\r\nHost:test.com\r\n\r\n", "uihuiuhiuhiuh"),
          new HttpTraceTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.UnparsableResponse,
          "HTTP/1.1 - Trace - 200 - Unparsable"
        ),
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:80 HTTP/1.1\r\nHost:test.com:80\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n"),
          new HttpTraceTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.NetworkError,
          "HTTP/1.1 - Trace - 200 - None"
        ),

      // HTTP1.1 via head via SSL
      new U(
          portCounter++,
          new TestServer()
            .AddExchange("CONNECT test.com:443 HTTP/1.1\r\nHost:test.com:443\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n")
            .AddExchange(null, "null"),
          new HttpHeadTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.NetworkError,
          "HTTP/1.1/SSL - Head - 200 - None",
          "https://test.com"
        ),
      new U(
          portCounter++,
          new TestServerRelay("brokenevent.com")
            .AddExchange("CONNECT brokenevent.com:443 HTTP/1.1\r\nHost:brokenevent.com:443\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n"),
          new HttpHeadTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.OK,
          "HTTP/1.1/SSL - Head - 200 - brokenevent.com",
          "https://brokenevent.com"
        ),
      new U(
          portCounter++,
          new TestServerRelay("brokenevent.com")
            .AddExchange("CONNECT microsoft.com:443 HTTP/1.1\r\nHost:microsoft.com:443\r\n\r\n", "HTTP/1.1 200 OK\r\n\r\n"),
          new HttpHeadTunnelTester(),
          HttpVersion.OneOne,
          ProxyCheckResult.SSLError,
          "HTTP/1.1/SSL - Head - 200 - SSL error",
          "https://microsoft.com"
        ),
    };

    [TestCaseSource(nameof(testData))]
    public async Task Test(U u)
    {
#pragma warning disable 4014
      if (u.Server != null)
        Task.Run(() => u.Server.Start(u.Port));
#pragma warning restore 4014

      try
      {

        ProxyChecker checker = new ProxyChecker
        {
          TargetUrl = new Uri(u.Target),
          TunnelTester = u.TunnelTester,
        };
        checker.AddProtocolChecker("http", new HttpConnectChecker { HttpVersion = u.Version });

        checker.Prepare();
        ProxyState state = await checker.CheckProxy(new ProxyInformation("127.0.0.1", (ushort)u.Port, "http"), CancellationToken.None);

        if (u.Server != null)
          Assert.IsNull(u.Server.Error);
        Assert.AreEqual(u.ExpectedResult, state.Result);
      }
      finally
      {
        u.Server?.Stop();
      }
    }
  }
}
