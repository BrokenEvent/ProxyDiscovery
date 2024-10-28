using System;
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
    /// Gets or sets the XPath expression to get the endpoint value (IP:Port)
    /// </summary>
    /// <remarks>
    /// <para>Relative to one of the nodes returned by <see cref="ProxyTablePath"/>.</para>
    /// <para>If not empty, overrides <see cref="IpPath"/> and <see cref="PortPath"/>.</para>
    /// </remarks>
    public string EndpointPath { get; set; }

    /// <summary>
    /// Gets or sets the XPath expression to get the value indicating whether the proxy supports HTTPS.
    /// </summary>
    /// <remarks>
    /// <para>Relative to one of the nodes returned by <see cref="ProxyTablePath"/>.</para>
    /// <para>May be <c>null</c>.</para>
    /// <para>Values "1", "yes", "true" and "+" (case-insensitive) are treated as <c>true</c>, other values are treated as <c>false</c>.</para>
    /// </remarks>
    public string IsHttpsPath { get; set; }

    /// <summary>
    /// Gets or sets the XPath expression to get the value indicating whether the proxy is google-passed.
    /// </summary>
    /// <remarks>
    /// <para>Relative to one of the nodes returned by <see cref="ProxyTablePath"/>.</para>
    /// <para>May be <c>null</c>.</para>
    /// <para>Values "1", "yes", "true" and "+" (case-insensitive) are treated as <c>true</c>, other values are treated as <c>false</c>.</para>
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

      if (string.IsNullOrWhiteSpace(EndpointPath))
      {
        if (string.IsNullOrWhiteSpace(IpPath))
          yield return "Either Endpoint XPath or IP XPath must be set";

        if (string.IsNullOrWhiteSpace(PortPath))
          yield return "Either Endpoint XPath or Port XPath must be set";
      }
    }

    private static string GetNodeContent(HtmlNode parent, string xPath, Action<string> onError, string entity)
    {
      HtmlNode node = parent.SelectSingleNode(xPath);
      if (node == null)
      {
        onError($"Failed to get {entity} node with '{xPath}'");
        return null;
      }

      string result = node.InnerText;
      if (!string.IsNullOrWhiteSpace(result))
        result = result.Trim();

      return result;
    }

    private static string GetNodeContent(HtmlNode parent, string xPath, string defaultValue = null)
    {
      if (string.IsNullOrWhiteSpace(xPath))
        return defaultValue;

      HtmlNode node = parent.SelectSingleNode(xPath);
      if (node == null)
        return defaultValue;

      string result = node.InnerText;
      if (string.IsNullOrWhiteSpace(result))
        return defaultValue;

      return result.Trim();
    }

    private static bool? GetNodeBoolContent(HtmlNode parent, string xPath)
    {
      if (string.IsNullOrWhiteSpace(xPath))
        return false;

      HtmlNode node = parent.SelectSingleNode(xPath);
      if (node == null)
        return null;

      return StringHelpers.ParseBool(node.InnerText);
    }

    private ProxyInformation ParseNode(HtmlNode node, Action<string> onError)
    {
      IPAddress address;
      ushort port;

      // do we use endpoint?
      if (!string.IsNullOrWhiteSpace(EndpointPath))
      {
        // get text
        string endpoint = GetNodeContent(node, EndpointPath, onError, "endpoint");
        if (endpoint == null)
          return null; // if we failed to get it

        // attempt to parse
        if (!StringHelpers.ParseEndPoint(endpoint, out address, out port))
        {
          onError($"Unable to parse endpoint. IP:Port expected, but '{endpoint}' encountered.");
          return null;
        }
      }
      else // or we have separate nodes for address and port?
      {
        // address
        string str = GetNodeContent(node, IpPath, onError, "address");
        if (str == null)
          return null; // if we failed to get it

        address = IPAddress.Parse(str); // there is no TryParse, so parsing error will produce an exception

        // port
        str = GetNodeContent(node, PortPath, onError, "port");
        if (str == null)
          return null; // if we failed to get it

        if (!ushort.TryParse(str, out port))
        {
          onError($"Unable to parse port. Number expected, but '{str}' encountered.");
          return null;
        }
      }

      return new ProxyInformation(
          address,
          port,
          GetNodeBoolContent(node, IsHttpsPath),
          GetNodeBoolContent(node, GooglePassedPath),
          GetNodeContent(node, ProtocolPath, DefaultProtocol),
          GetNodeContent(node, NamePath),
          GetNodeContent(node, CountryPath),
          GetNodeContent(node, CityPath)
        );
    }

    /// <inheritdoc />
    public IEnumerable<ProxyInformation> ParseContent(string content, Action<string> onError)
    {
      HtmlDocument doc = new HtmlDocument();
      doc.LoadHtml(content);

      HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(ProxyTablePath);

      if (nodes == null)
      {
        onError($"Failed to get nodes with '{ProxyTablePath}'");
        yield break;
      }

      foreach (HtmlNode node in nodes)
      {
        ProxyInformation info = null;
        try
        {
          info = ParseNode(node, onError);
        }
        catch (Exception e)
        {
          onError(e.Message);
        }

        if (info != null)
          yield return info;
      }
    }

    public override string ToString()
    {
      return "HTML Parser";
    }
  }
}
