using System.Collections.Generic;
using System.Net;

using BrokenEvent.ProxyDiscovery.Helpers;
using BrokenEvent.ProxyDiscovery.Interfaces;

using HtmlAgilityPack;

namespace BrokenEvent.ProxyDiscovery.Parsers
{
  /// <summary>
  /// Gets the proxy list from the HTML page content.
  /// </summary>
  public sealed class HtmlProxyListParser: IProxyListParser
  {
    /// <summary>
    /// Gets or sets the XPath expression for proxies list. 
    /// </summary>
    /// <remarks>Must return multiple nodes and is relative to page root.</remarks>
    public string ProxyTablePath { get; set; }

    /// <summary>
    /// Gets or sets the XPath expression to get the IP address. 
    /// </summary>
    /// <remarks>Relative to one of the nodes returned by <see cref="ProxyTablePath"/>.</remarks>
    public string IpPath { get; set; }

    /// <summary>
    /// Gets or sets the XPath expression to get the port number.
    /// </summary>
    /// <remarks>Relative to one of the nodes returned by <see cref="ProxyTablePath"/>.</remarks>
    public string PortPath { get; set; }

    /// <summary>
    /// Gets or sets the XPath expression to get the value indicating whether the proxy supports HTTPS.
    /// </summary>
    /// <remarks>
    /// <para>Relative to one of the nodes returned by <see cref="ProxyTablePath"/>.</para>
    /// <para>May be <c>null</c>.</para>
    /// </remarks>
    public string IsHttpsPath { get; set; }

    /// <summary>
    /// Gets or sets the XPath expression to get the value indicating whether the proxy is google-passed.
    /// </summary>
    /// <remarks>
    /// <para>Relative to one of the nodes returned by <see cref="ProxyTablePath"/>.</para>
    /// <para>May be <c>null</c>.</para>
    /// </remarks>
    public string GooglePassedPath { get; set; }

    /// <summary>
    /// Gets or sets the XPath expression to get the protocol (http, socks4, socks5, etc.) of the proxy.
    /// </summary>
    /// <remarks>
    /// <para>Relative to one of the nodes returned by <see cref="ProxyTablePath"/>.</para>
    /// <para>May be <c>null</c>.</para>
    /// </remarks>
    public string ProtocolPath { get; set; }

    /// <summary>
    /// Gets or sets the default protocol value (http, socks4, socks5, etc.). This is used when we know all the proxies has the same protocol.
    /// </summary>
    /// <remarks>May be <c>null</c>.</remarks>
    public string DefaultProtocol { get; set; }

    /// <summary>
    /// Gets or sets the XPath expression to get the name of the proxy.
    /// </summary>
    /// <remarks>
    /// <para>Relative to one of the nodes returned by <see cref="ProxyTablePath"/>.</para>
    /// <para>May be <c>null</c>.</para>
    /// </remarks>
    public string NamePath { get; set; }

    /// <summary>
    /// Gets or sets the XPath expression to get the country of the proxy.
    /// </summary>
    /// <remarks>
    /// <para>Relative to one of the nodes returned by <see cref="ProxyTablePath"/>.</para>
    /// <para>May be <c>null</c>.</para>
    /// </remarks>
    public string CountryPath { get; set; }

    /// <summary>
    /// Gets or sets the XPath expression to get the city of the proxy.
    /// </summary>
    /// <remarks>
    /// <para>Relative to one of the nodes returned by <see cref="ProxyTablePath"/>.</para>
    /// <para>May be <c>null</c>.</para>
    /// </remarks>
    public string CityPath { get; set; }

    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      if (string.IsNullOrWhiteSpace(ProxyTablePath))
        yield return "Proxy table XPath is missing";

      if (string.IsNullOrWhiteSpace(IpPath))
        yield return "IP address XPath is missing";

      if (string.IsNullOrWhiteSpace(PortPath))
        yield return "Port XPath is missing";
    }

    /// <inheritdoc />
    public IEnumerable<ProxyInformation> ParseContent(string content)
    {
      HtmlDocument doc = new HtmlDocument();
      doc.LoadHtml(content);

      HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(ProxyTablePath);

      foreach (HtmlNode node in nodes)
      {
        yield return new ProxyInformation(
            IPAddress.Parse(node.SelectSingleNode(IpPath).InnerText),
            ushort.Parse(node.SelectSingleNode(PortPath).InnerText),
            IsHttpsPath == null ? (bool?)null : StringHelpers.ParseBool(node.SelectSingleNode(IsHttpsPath).InnerText),
            GooglePassedPath == null ? (bool?)null : StringHelpers.ParseBool(node.SelectSingleNode(GooglePassedPath).InnerText),
            ProtocolPath == null ? DefaultProtocol : node.SelectSingleNode(ProtocolPath).InnerText.ToLower(),
            NamePath == null ? null : node.SelectSingleNode(NamePath).InnerText,
            CountryPath == null ? null : node.SelectSingleNode(CountryPath).InnerText,
            CityPath == null ? null : node.SelectSingleNode(CityPath).InnerText
          );
      }
    }
  }
}
