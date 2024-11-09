using System.Text;

using BrokenEvent.ProxyDiscovery.Checkers;
using BrokenEvent.ProxyDiscovery.Helpers;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class HttpRequestBuilderTests
  {
    public class U
    {
      public string Expected { get; }

      public HttpVersion Version { get; }
      public string Method { get; }
      public string Host { get; }
      public int? Port { get; }
      public string Resource { get; }

      public U(string expected, HttpVersion version, string method, string host, int? port = null, string resource = null)
      {
        Expected = expected;
        Version = version;
        Method = method;
        Host = host;
        Port = port;
        Resource = resource;
      }

      public override string ToString()
      {
        return $"{Version} {Method} {Host} {Port} {Resource} → {Expected}";
      }
    }

    public static readonly U[] testData = new U[]
    {
      new U("CONNECT test.com HTTP/1.0\r\n\r\n", HttpVersion.OneZero, "CONNECT", "test.com"), 
      new U("CONNECT test.com HTTP/1.1\r\nHost:test.com\r\n\r\n", HttpVersion.OneOne, "CONNECT", "test.com"), 
      new U("CONNECT test.com:8080 HTTP/1.0\r\n\r\n", HttpVersion.OneZero, "CONNECT", "test.com", 8080), 
      new U("CONNECT test.com:8080 HTTP/1.1\r\nHost:test.com:8080\r\n\r\n", HttpVersion.OneOne, "CONNECT", "test.com", 8080),

      new U("GET /index.html HTTP/1.0\r\n\r\n", HttpVersion.OneZero, "GET", "test.com", null, "/index.html"),
      new U("GET /index.html HTTP/1.1\r\nHost:test.com\r\n\r\n", HttpVersion.OneOne, "GET", "test.com", null, "/index.html"),
      new U("GET /index.html HTTP/1.0\r\n\r\n", HttpVersion.OneZero, "GET", "test.com", 8080, "/index.html"),
      new U("GET /index.html HTTP/1.1\r\nHost:test.com:8080\r\n\r\n", HttpVersion.OneOne, "GET", "test.com", 8080, "/index.html"),
    };

    [TestCaseSource(nameof(testData))]
    public void TestRequestBuilder(U u)
    {
      byte[] bytes = HttpRequestBuilder.BuildRequest(u.Version, u.Method, u.Host, u.Port, u.Resource);

      Assert.AreEqual(u.Expected, Encoding.ASCII.GetString(bytes));
    }
  }
}
