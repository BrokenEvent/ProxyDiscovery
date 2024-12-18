﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BrokenEvent.ProxyDiscovery.Parsers
{
  /// <summary>
  /// Gets the proxy list by splitting the content by lines and parsing each one with regex.
  /// </summary>
  public sealed class LineRegexProxyListParser: AbstractProxyListParser
  {
    /// <summary>
    /// Creates an instance of line-based regex proxy list parser.
    /// </summary>
    /// <param name="expression">Regex to parse a single line. For details see remarks of <see cref="Expression"/>.</param>
    public LineRegexProxyListParser(string expression)
    {
      Expression = expression;
    }

    /// <summary>
    /// Gets or sets the regular expression to parse a single line.
    /// </summary>
    /// <remarks>
    /// Parser uses named regular expression groups (<c>(?&lt;name&gt;.\)</c>) to get the data. Group names are:
    /// <list type="table">
    /// <item><term>address</term><description>IP address of the proxy. Mandatory.</description></item>
    /// <item><term>port</term><description>Port number of the proxy. Mandatory.</description></item>
    /// <item><term>https</term><description>Whether the proxy supports HTTPS. Optional.</description></item>
    /// <item><term>google</term><description>Whether the proxy is google-passed, i.e. can be used for google search. Optional</description></item>
    /// <item><term>protocol</term><description>Protocol (http, socks4, socks5, etc.) of the proxy. Optional.</description></item>
    /// <item><term>name</term><description>Name of the proxy. Optional.</description></item>
    /// <item><term>country</term><description>Country of the proxy. Optional.</description></item>
    /// <item><term>city</term><description>City of the proxy. Optional.</description></item>
    /// </list>
    /// For boolean groups like "https" and "google" missing group is treated as "unknown". For value "1", "yes", "true" and "+"
    /// (case-insensitive) are treated as <c>true</c>, other values are treated as <c>false</c>.
    /// </remarks>
    public string Expression { get; set; }

    /// <inheritdoc />
    public override IEnumerable<string> Validate()
    {
      if (string.IsNullOrWhiteSpace(Expression))
      {
        yield return "Line regex cannot be empty";
        yield break;
      }

      Regex regex = null;
      string exceptionMessage = null;
      try
      {
        regex = new Regex(Expression);
      }
      catch (Exception e)
      {
        exceptionMessage = e.Message;
      }

      // could not parse
      if (exceptionMessage != null)
      {
        yield return exceptionMessage;
        yield break;
      }

      if (string.IsNullOrWhiteSpace(DefaultProtocol) && regex.GroupNumberFromName("protocol") == -1)
        yield return "Neither the default protocol, nor <protocol> regex group not specified.";
      if (regex.GroupNumberFromName("address") == -1)
        yield return "Regex group <address> not specified";
      if (regex.GroupNumberFromName("port") == -1)
        yield return "Regex group <port> not specified";
    }

    private Match CreateMatch(string line, Action<string> onError)
    {
      if (string.IsNullOrWhiteSpace(line))
        return null;

      try
      {
        Match match = Regex.Match(line, Expression);

        if (!match.Success)
          return null;

        return match;
      }
      catch (Exception e)
      {
        onError($"Unable to parse line '{line}': {e.Message}");
        return null;
      }
    }

    /// <inheritdoc />
    public override IEnumerable<ProxyInformation> ParseContent(string content, Action<string> onError)
    {
      string[] lines = content.Split(new string[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

      foreach (string line in lines)
      {
        Match match = CreateMatch(line, onError);
        if (match == null)
          continue;

        Group groupAddress = match.Groups["address"];
        Group groupPort = match.Groups["port"];
        Group groupSsl = match.Groups["ssl"];
        Group groupGoogle = match.Groups["google"];
        Group groupProtocol = match.Groups["protocol"];
        Group groupName = match.Groups["name"];
        Group groupCountry = match.Groups["country"];
        Group groupCity = match.Groups["city"];

        if (!groupAddress.Success || !groupPort.Success)
        {
          onError($"Line '{line}' does not contain address and port");
          continue;
        }

        yield return new ProxyInformation(
            groupAddress.Value,
            ushort.Parse(groupPort.Value),
            groupProtocol.Success ? groupProtocol.Value.ToLower() : DefaultProtocol,
            groupSsl.Success ? !string.IsNullOrWhiteSpace(groupSsl.Value) : DefaultSSL,
            groupGoogle.Success ? !string.IsNullOrWhiteSpace(groupGoogle.Value) : DefaultGoogle,
            groupName.Success ? groupName.Value : null,
            groupCountry.Success ? groupCountry.Value : null,
            groupCity.Success ? groupCity.Value : null);
      }
    }

    public override string ToString()
    {
      return "Line Regex Parser";
    }
  }
}
