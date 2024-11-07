using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using BrokenEvent.ProxyDiscovery.Helpers;

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
    /// <param name="lineRegex">Regex to parse single line. For details see remarks of <see cref="LineRegex"/>.</param>
    public LineRegexProxyListParser(string lineRegex)
    {
      LineRegex = lineRegex;
    }

    /// <summary>
    /// Gets or sets the regex to parse single line.
    /// </summary>
    /// <remarks>
    /// Parser uses named regular expression groups to get the data. Group names are:
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
    public string LineRegex { get; set; }

    private string ValidateRegex()
    {
      try
      {
        // ReSharper disable once ObjectCreationAsStatement
        new Regex(LineRegex);
        return null;
      }
      catch (Exception e)
      {
        return e.Message;
      }
    }

    /// <inheritdoc />
    public override IEnumerable<string> Validate()
    {
      if (string.IsNullOrWhiteSpace(LineRegex))
        yield return "Line regex cannot be empty";
      else
      {
        string error = ValidateRegex();
        if (error != null)
          yield return error;
      }
    }

    private Match CreateMatch(string line, Action<string> onError)
    {
      if (string.IsNullOrWhiteSpace(line))
        return null;

      try
      {
        Match match = Regex.Match(line, LineRegex);

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
        Group groupHttps = match.Groups["https"];
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
            groupHttps.Success ? StringHelpers.ParseBool(groupHttps.Value) : DefaultHttps,
            groupGoogle.Success ? StringHelpers.ParseBool(groupGoogle.Value) : DefaultGoogle,
            groupProtocol.Success ? groupProtocol.Value.ToLower() : DefaultProtocol,
            groupName.Success ? groupName.Value : null,
            groupCountry.Success ? groupCountry.Value : null,
            groupCity.Success ? groupCity.Value : null
          );
      }
    }

    public override string ToString()
    {
      return "Line Regex Parser";
    }
  }
}
