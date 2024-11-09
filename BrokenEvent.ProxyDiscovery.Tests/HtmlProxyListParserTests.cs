using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using BrokenEvent.ProxyDiscovery.Parsers;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class HtmlProxyListParserTests
  {
    private static string LoadResource(string name)
    {
      using (Stream stream = Assembly.GetCallingAssembly().GetManifestResourceStream($"BrokenEvent.ProxyDiscovery.Tests.Data.{name}"))
      {
        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer);
      }
    }

    public class U
    {
      public string Resource { get; }
      public HtmlProxyListParser Parser { get; }
      public bool Success { get; }

      public U(string resource, HtmlProxyListParser parser, bool success = true)
      {
        Resource = resource;
        Parser = parser;
        Success = success;
      }

      public override string ToString()
      {
        return Resource;
      }
    }

    public static readonly U[] simpleTestsData = new U[]
    {
      new U("msword.htm", new HtmlProxyListParser { ProxyTablePath = "//table/tr", IpPath = "td[1]/p", PortPath = "td[2]/p", DefaultProtocol = "http" }),
      new U("msword.htm", new HtmlProxyListParser { ProxyTablePath = "//table/tr", IpPath = "td[1]/p", PortPath = "td[1]/p", DefaultProtocol = "http" }, false),
      new U("msword.htm", new HtmlProxyListParser { ProxyTablePath = "//table/tr", IpPath = "td[1]/p", PortPath = "td[2]/p", ProtocolPath = "td[3]/p" }),
    };

    [TestCaseSource(nameof(simpleTestsData))]
    public void SimpleTest(U u)
    {
      LogCollector lc = new LogCollector();
      List<ProxyInformation> list = new List<ProxyInformation>(u.Parser.ParseContent(LoadResource(u.Resource), lc.AddLog));

      if (!u.Success)
      {
        Assert.AreEqual(0, list.Count);
        return;
      }

      Assert.AreEqual(0, lc.Errors.Count);
      Assert.AreEqual(3, list.Count);
      Assert.AreEqual("192.168.0.1", list[0].Address.ToString());
      Assert.AreEqual(8081, list[0].Port);
      Assert.AreEqual("http", list[0].Protocol);
      Assert.AreEqual("192.168.0.2", list[1].Address.ToString());
      Assert.AreEqual(8081, list[1].Port);
      Assert.AreEqual("http", list[1].Protocol);
      Assert.AreEqual("192.168.0.5", list[2].Address.ToString());
      Assert.AreEqual(3128, list[2].Port);
      Assert.AreEqual("http", list[2].Protocol);
    }
  }
}
