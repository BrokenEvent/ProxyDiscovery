using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Sources;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class WebProxyListSourceTests
  {
    public class U
    {
      public int Port { get; }
      public TestServer Server { get; }
      public WebProxyListSource Source { get; }
      public List<string> Content { get; } = new List<string>();
      public string Description { get; }

      public U(int port, TestServer server, WebProxyListSource source, string description)
      {
        Port = port;
        Server = server;
        Source = source;
        Description = description;
      }

      public override string ToString()
      {
        return Description;
      }
    }

    private static int portNumber = 22001;

    public static readonly U[] webTestsData = new U[]
    {
      // single requests
      new U(
          portNumber,
          new TestServer()
            .AddExchange(
              $"GET /list HTTP/1.1\r\nHost: 127.0.0.1:{portNumber}\r\nConnection: Keep-Alive\r\n\r\n",
              "HTTP/1.0 200 OK",
              "test content"
            ),
          new WebProxyListSource($"http://127.0.0.1:{portNumber++}/list"),
          "single-paged get"
        )
        {
          Content = { "test content" }
        },
      new U(
          portNumber,
          new TestServer()
            .AddExchange(
              $"POST /list HTTP/1.1\r\nContent-Type: application/x-www-form-urlencoded\r\nHost: 127.0.0.1:{portNumber}\r\nContent-Length: 8\r\nConnection: Keep-Alive\r\n\r\ntestId=1",
              "HTTP/1.0 200 OK",
              "test content"
            ),
          new WebProxyListSource($"http://127.0.0.1:{portNumber++}/list")
          {
            HttpMethod = "POST",
            FormData = "testId=1"
          },
          "single-paged post"
        )
        {
          Content = { "test content" }
        },

      // multipage requests
      new U(
          portNumber,
          new TestServer()
            .AddExchange(
              $"GET /list HTTP/1.1\r\nHost: 127.0.0.1:{portNumber}\r\nConnection: Keep-Alive\r\n\r\n",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              "test content1"
            )
            .AddExchange(
              $"GET /list/2 HTTP/1.1\r\nHost: 127.0.0.1:{portNumber}\r\nConnection: Keep-Alive\r\n\r\n",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              "test content2"
            )
            .AddExchange(
              $"GET /list/3 HTTP/1.1\r\nHost: 127.0.0.1:{portNumber}\r\nConnection: Keep-Alive\r\n\r\n",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              ""
            ),
          new WebProxyListSource($"http://127.0.0.1:{portNumber++}/list[/$]"),
          "multipage get (no first page)"
        )
        {
          Content = { "test content1", "test content2" }
        },
      new U(
          portNumber,
          new TestServer()
            .AddExchange(
              $"GET /list/1 HTTP/1.1\r\nHost: 127.0.0.1:{portNumber}\r\nConnection: Keep-Alive\r\n\r\n",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              "test content1"
            )
            .AddExchange(
              $"GET /list/2 HTTP/1.1\r\nHost: 127.0.0.1:{portNumber}\r\nConnection: Keep-Alive\r\n\r\n",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              "test content2"
            )
            .AddExchange(
              $"GET /list/3 HTTP/1.1\r\nHost: 127.0.0.1:{portNumber}\r\nConnection: Keep-Alive\r\n\r\n",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              ""
            ),
          new WebProxyListSource($"http://127.0.0.1:{portNumber++}/list[/$]") {SkipNumberForFirstPage = false},
          "multipage get (with first page)"
        )
        {
          Content = { "test content1", "test content2" }
        },
      new U(
          portNumber,
          new TestServer()
            .AddExchange(
              $"POST /list HTTP/1.1\r\nContent-Type: application/x-www-form-urlencoded\r\nHost: 127.0.0.1:{portNumber}\r\nContent-Length: 8\r\nConnection: Keep-Alive\r\n\r\ntestId=1",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              "test content1"
            )
            .AddExchange(
              $"POST /list HTTP/1.1\r\nContent-Type: application/x-www-form-urlencoded\r\nHost: 127.0.0.1:{portNumber}\r\nContent-Length: 15\r\nConnection: Keep-Alive\r\n\r\ntestId=1&page=2",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              "test content2"
            )
            .AddExchange(
              $"POST /list HTTP/1.1\r\nContent-Type: application/x-www-form-urlencoded\r\nHost: 127.0.0.1:{portNumber}\r\nContent-Length: 15\r\nConnection: Keep-Alive\r\n\r\ntestId=1&page=3",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              ""
            ),
          new WebProxyListSource($"http://127.0.0.1:{portNumber++}/list")
            {
              HttpMethod = "POST",
              FormData = "testId=1[&page=$]"
            },
          "multipage post (no first page)"
        )
        {
          Content = { "test content1", "test content2" }
        },
      new U(
          portNumber,
          new TestServer()
            .AddExchange(
              $"POST /list HTTP/1.1\r\nContent-Type: application/x-www-form-urlencoded\r\nHost: 127.0.0.1:{portNumber}\r\nContent-Length: 15\r\nConnection: Keep-Alive\r\n\r\ntestId=1&page=1",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              "test content1"
            )
            .AddExchange(
              $"POST /list HTTP/1.1\r\nContent-Type: application/x-www-form-urlencoded\r\nHost: 127.0.0.1:{portNumber}\r\nContent-Length: 15\r\nConnection: Keep-Alive\r\n\r\ntestId=1&page=2",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              "test content2"
            )
            .AddExchange(
              $"POST /list HTTP/1.1\r\nContent-Type: application/x-www-form-urlencoded\r\nHost: 127.0.0.1:{portNumber}\r\nContent-Length: 15\r\nConnection: Keep-Alive\r\n\r\ntestId=1&page=3",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              ""
            ),
          new WebProxyListSource($"http://127.0.0.1:{portNumber++}/list")
          {
            HttpMethod = "POST",
            FormData = "testId=1[&page=$]",
            SkipNumberForFirstPage = false
          },
          "multipage post (with first page)"
        )
        {
          Content = { "test content1", "test content2" }
        },
      new U(
          portNumber,
          new TestServer()
            .AddExchange(
              $"PUT /list HTTP/1.1\r\nContent-Type: application/x-www-form-urlencoded\r\nHost: 127.0.0.1:{portNumber}\r\nContent-Length: 15\r\nConnection: Keep-Alive\r\n\r\ntestId=1&page=1",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              "test content1"
            )
            .AddExchange(
              $"PUT /list HTTP/1.1\r\nContent-Type: application/x-www-form-urlencoded\r\nHost: 127.0.0.1:{portNumber}\r\nContent-Length: 15\r\nConnection: Keep-Alive\r\n\r\ntestId=1&page=2",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              "test content2"
            )
            .AddExchange(
              $"PUT /list HTTP/1.1\r\nContent-Type: application/x-www-form-urlencoded\r\nHost: 127.0.0.1:{portNumber}\r\nContent-Length: 15\r\nConnection: Keep-Alive\r\n\r\ntestId=1&page=3",
              "HTTP/1.0 200 OK\r\nConnection: Keep-Alive",
              ""
            ),
          new WebProxyListSource($"http://127.0.0.1:{portNumber++}/list")
          {
            HttpMethod = "PUT",
            FormData = "testId=1[&page=$]",
            SkipNumberForFirstPage = false
          },
          "multipage put (with first page)"
        )
        {
          Content = { "test content1", "test content2" }
        },
    };

    [TestCaseSource(nameof(webTestsData))]
    public async Task Test(U u)
    {
#pragma warning disable 4014
      if (u.Server != null)
        Task.Run(() => u.Server.Start(u.Port));
#pragma warning restore 4014

      LogCollector log = new LogCollector();

      try
      {
        string content;
        if (u.Source.HasPages)
        {
          int page = 0;
          while (true)
          {
            content = await u.Source.GetContentAsync(CancellationToken.None, page + 1, log.AddLog);

            if (string.IsNullOrWhiteSpace(content))
              break;

            Assert.AreEqual(u.Content[page], content);

            page++;
          }

          Assert.AreEqual(u.Content.Count, page);
        }
        else
        {
          Assert.AreEqual(1, u.Content.Count);

          content = await u.Source.GetContentAsync(CancellationToken.None, 0, log.AddLog);

          Assert.AreEqual(u.Content[0], content);
        }

        Assert.IsNull(u.Server.Error);
      }
      finally
      {
        u.Server?.Stop();
      }
    }
  }
}
