using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using BrokenEvent.ProxyDiscovery.Parsers;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class CsvProxyListParserTests
  {
    private static string LoadResource(string name)
    {
      using (Stream stream = Assembly.GetCallingAssembly().GetManifestResourceStream($"BrokenEvent.ProxyDiscovery.Tests.Data.{name}.csv"))
      {
        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer);
      }
    }

    public class LogCollector
    {
      public List<string> Errors { get; } = new List<string>();

      public void AddLog(string s)
      {
        Errors.Add(s);
      }
    }

    public class U
    {
      public string Resource { get; }
      public CsvProxyListParser Parser { get; }
      public string Name { get; }

      public U(string resource, CsvProxyListParser parser, string name = null)
      {
        Resource = resource;
        Parser = parser;
        Name = name;
      }

      public override string ToString()
      {
        return Name == null ? Resource : $"{Resource} ({Name})";
      }
    }

    public static readonly U[] simpleTestsData = new U[]
    {
      new U("comma_noheader_ip_port", new CsvProxyListParser { IpColumn = 0, PortColumn = 1, DefaultProtocol = "http", Separator = CsvSeparator.Comma }),
      new U("semicolon_noheader_ip_port", new CsvProxyListParser { IpColumn = 0, PortColumn = 1, DefaultProtocol = "http", Separator = CsvSeparator.Semicolon }),
      new U("comma_header_ip_port", new CsvProxyListParser { IpColumn = 0, PortColumn = 1, DefaultProtocol = "http", SkipHeader = true, Separator = CsvSeparator.Comma }),
      new U("semicolon_header_ip_port", new CsvProxyListParser { IpColumn = 0, PortColumn = 1, DefaultProtocol = "http", SkipHeader = true, Separator = CsvSeparator.Semicolon }),

      new U("comma_noheader_ip_port", new CsvProxyListParser { IpColumn = 0, PortColumn = 1, DefaultProtocol = "http" }, "detect"),
      new U("semicolon_noheader_ip_port", new CsvProxyListParser { IpColumn = 0, PortColumn = 1, DefaultProtocol = "http" }, "detect"),
      new U("comma_header_ip_port", new CsvProxyListParser { IpColumn = 0, PortColumn = 1, DefaultProtocol = "http", SkipHeader = true }, "detect"),
      new U("semicolon_header_ip_port", new CsvProxyListParser { IpColumn = 0, PortColumn = 1, DefaultProtocol = "http", SkipHeader = true }, "detect"),

      new U("comma_noheader_endpoint", new CsvProxyListParser { EndpointColumn = 0, DefaultProtocol = "http", Separator = CsvSeparator.Comma }),
      new U("semicolon_noheader_endpoint", new CsvProxyListParser { EndpointColumn = 0, DefaultProtocol = "http", Separator = CsvSeparator.Semicolon }),
      new U("comma_header_endpoint", new CsvProxyListParser { EndpointColumn = 0, DefaultProtocol = "http", SkipHeader = true, Separator = CsvSeparator.Comma }),
      new U("semicolon_header_endpoint", new CsvProxyListParser { EndpointColumn = 0, DefaultProtocol = "http", SkipHeader = true, Separator = CsvSeparator.Semicolon }),

      new U("comma_header_endpoint_protocol", new CsvProxyListParser { EndpointColumn = 0, ProtocolColumn = 1, SkipHeader = true, Separator = CsvSeparator.Comma }),
      new U("semicolon_noheader_ip_port_protocol", new CsvProxyListParser { IpColumn = 0, PortColumn = 1, ProtocolColumn = 2, Separator = CsvSeparator.Semicolon }),
    };

    [TestCaseSource(nameof(simpleTestsData))]
    public void SimpleTest(U u)
    {
      LogCollector lc = new LogCollector();
      List<ProxyInformation> list = new List<ProxyInformation>(u.Parser.ParseContent(LoadResource(u.Resource), lc.AddLog));

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

    public static readonly U[] fullTestsData = new U[]
    {
      new U(
        "semicolon_header_ip_port_full",
        new CsvProxyListParser
        {
          IpColumn = 0,
          PortColumn = 2,
          CountryColumn = 3,
          CityColumn = 4,
          ProtocolColumn = 5,
          IsHttpsColumn = 7,
          NameColumn = 8,
          GooglePassedColumn = 9,
        }),
      new U(
        "semicolon_header_endpoint_spec",
        new CsvProxyListParser
        {
          EndpointColumn = 3,
          CountryColumn = 1,
          CityColumn = 2,
          ProtocolColumn = 4,
          IsHttpsColumn = 6,
          NameColumn = 7,
          GooglePassedColumn = 8,
        }),
    };

    [TestCaseSource(nameof(fullTestsData))]
    public void FullTest(U u)
    {
      LogCollector lc = new LogCollector();
      List<ProxyInformation> list = new List<ProxyInformation>(u.Parser.ParseContent(LoadResource(u.Resource), lc.AddLog));

      Assert.AreEqual(1, lc.Errors.Count);  // header cannot be parsed

      Assert.AreEqual(3, list.Count);
      Assert.AreEqual("192.168.0.1", list[0].Address.ToString());
      Assert.AreEqual(3128, list[0].Port);
      Assert.AreEqual("http", list[0].Protocol);
      Assert.AreEqual("USA", list[0].Country);
      Assert.AreEqual("Washington", list[0].City);
      Assert.AreEqual("President Personal Server", list[0].Name);
      Assert.IsTrue(list[0].IsHttps);
      Assert.IsFalse(list[0].GooglePassed);

      Assert.AreEqual(3, list.Count);
      Assert.AreEqual("192.168.0.2", list[1].Address.ToString());
      Assert.AreEqual(8081, list[1].Port);
      Assert.AreEqual("http", list[1].Protocol);
      Assert.AreEqual("USA", list[1].Country);
      Assert.AreEqual("Washington", list[1].City);
      Assert.AreEqual("Common Militrary proxy", list[1].Name);
      Assert.IsTrue(list[1].IsHttps);
      Assert.IsFalse(list[1].GooglePassed);

      Assert.AreEqual(3, list.Count);
      Assert.AreEqual("192.168.198.50", list[2].Address.ToString());
      Assert.AreEqual(6666, list[2].Port);
      Assert.IsNull(list[2].Protocol);
      Assert.AreEqual("Russia", list[2].Country);
      Assert.AreEqual("Moscow", list[2].City);
      Assert.IsNull(list[2].Name);
      Assert.IsFalse(list[2].IsHttps);
      Assert.IsTrue(list[2].GooglePassed);
    }
  }
}
