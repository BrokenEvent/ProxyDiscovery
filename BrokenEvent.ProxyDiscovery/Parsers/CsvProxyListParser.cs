using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using BrokenEvent.ProxyDiscovery.Helpers;
using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Parsers
{
  /// <summary>
  /// Parses the CSV content to proxy list.
  /// </summary>
  public sealed class CsvProxyListParser: IProxyListParser
  {
    private static readonly string[] LineDelimiters = new string[] { "\r", "\n", "\r\n" };

    /// <summary>
    /// Gets or sets the value indicating whether to skip first line (column headers).
    /// </summary>
    public bool SkipHeader { get; set; }

    /// <summary>
    /// Gets or sets the CSV column separator. Default for CSV format is comma, but in some countries (ru, ua, etc.)
    /// comma is used as decimal point, so CSV uses semicolon as column separator.
    /// </summary>
    public CsvSeparator Separator { get; set; }

    /// <summary>
    /// Gets or sets zero-based number of column containing the IP address.
    /// </summary>
    public int IpColumn { get; set; }

    /// <summary>
    /// Gets or sets zero-based number of column containing the port number.
    /// </summary>
    public int PortColumn { get; set; }

    /// <summary>
    /// Gets or sets zero-based number of column containing the endpoint (IP:Port).
    /// </summary>
    /// <remarks>If set, overrides <see cref="IpColumn"/> and <see cref="PortColumn"/>.</remarks>
    public int? EndpointColumn { get; set; }

    /// <summary>
    /// Gets or sets zero-based number of column containing the value indicating whether the proxy server supports HTTPS.
    /// </summary>
    /// <remarks>Values "1", "yes", "true" and "+" (case-insensitive) are treated as <c>true</c>, other values are treated as <c>false</c></remarks>
    public int? IsHttpsColumn { get; set; }

    /// <summary>
    /// Gets or sets zero-based number of column containing the value indicating whether the proxy server is google-passed.
    /// </summary>
    /// <remarks>Values "1", "yes", "true" and "+" (case-insensitive) are treated as <c>true</c>, other values are treated as <c>false</c></remarks>
    public int? GooglePassedColumn { get; set; }

    /// <summary>
    /// Gets or sets zero-based number of column containing the protocol (http, socks4, socks5, etc.) of the proxy.
    /// </summary>
    public int? ProtocolColumn { get; set; }

    /// <summary>
    /// Gets or sets the default protocol value (http, socks4, socks5, etc.). This is used when we know all the proxies has the same protocol.
    /// </summary>
    /// <remarks>May be <c>null</c>.</remarks>
    public string DefaultProtocol { get; set; }

    /// <summary>
    /// Gets or sets zero-based number of column containing the proxy server name.
    /// </summary>
    public int? NameColumn { get; set; }

    /// <summary>
    /// Gets or sets zero-based number of column containing country of the proxy.
    /// </summary>
    public int? CountryColumn { get; set; }

    /// <summary>
    /// Gets or sets zero-based number of column containing city of the proxy.
    /// </summary>
    public int? CityColumn { get; set; }

    public IEnumerable<string> Validate()
    {
      if (!EndpointColumn.HasValue && IpColumn == PortColumn)
        yield return "IP column and Port column numbers cannot be equal";
    }

    private static string GetRow(IReadOnlyList<string> cols, int index, Action<string> onError, string entity)
    {
      if (index < 0 || index >= cols.Count)
      {
        onError($"Unable to get {entity} by column {index}. It is out of columns range ({cols.Count})");
        return null;
      }

      return cols[index];
    }

    private static string GetRow(IReadOnlyList<string> cols, int? index, string defaultValue = null)
    {
      if (!index.HasValue)
        return defaultValue;

      if (index.Value < 0 || index.Value >= cols.Count)
        return defaultValue;

      return cols[index.Value];
    }

    private static bool GetRowBool(IReadOnlyList<string> cols, int? index)
    {
      if (!index.HasValue)
        return false;

      if (index.Value < 0 || index.Value >= cols.Count)
        return false;

      return StringHelpers.ParseBool(cols[index.Value]);
    }

    private ProxyInformation ParseRow(string line, Action<string> onError, char separator)
    {
      List<string> cols = CsvParser.Split(line, separator);

      IPAddress address;
      ushort port;

      // do we use endpoint?
      if (EndpointColumn.HasValue)
      {
        // get text
        string endpoint = GetRow(cols, EndpointColumn.Value, onError, "endpoint");
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
        string str = GetRow(cols, IpColumn, onError, "address");
        if (str == null)
          return null; // if we failed to get it

        address = IPAddress.Parse(str); // there is no TryParse, so parsing error will produce an exception

        // port
        str = GetRow(cols, PortColumn, onError, "port");
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
          GetRowBool(cols, IsHttpsColumn),
          GetRowBool(cols, GooglePassedColumn),
          GetRow(cols, ProtocolColumn, DefaultProtocol),
          GetRow(cols, NameColumn),
          GetRow(cols, CountryColumn),
          GetRow(cols, CityColumn)
        );
    }

    private static char GetSeparator(string firstLine, CsvSeparator separator)
    {
      if (separator == CsvSeparator.Comma)
        return ',';
      if (separator == CsvSeparator.Semicolon)
        return ';';

      // semicolon is less likely to be encountered in value.
      string[] strings = firstLine.Split(';');
      if (strings.Length == 1)
        return ','; // didn't split well enough

      return ';';
    }

    public IEnumerable<ProxyInformation> ParseContent(string content, Action<string> onError)
    {
      string[] rows = content.Split(LineDelimiters, StringSplitOptions.RemoveEmptyEntries);

      char separator = ',';

      for (int i = 0; i < rows.Length; i++)
      {
        string row = rows[i];

        if (i == 0)
        {
          separator = GetSeparator(row, Separator);

          if (SkipHeader)
            continue;
        }

        ProxyInformation info = null;
        try
        {
          info = ParseRow(row, onError, separator);
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
      return "CSV Parser";
    }
  }

  public enum CsvSeparator
  {
    /// <summary>
    /// Attempts to detect separator (comma or semicolon) based on content. If the content contains floating point numbers, the result may be wrong.
    /// </summary>
    Detect,
    /// <summary>
    /// Separator between columns is be comma (,). Default for CSV format.
    /// </summary>
    Comma,
    /// <summary>
    /// Separator between columns is semicolon (;). Used by CSV formats in cultures where comma is used as decimal point (ru, ua, etc.).
    /// </summary>
    Semicolon,
  }
}
