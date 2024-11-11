using System;
using System.Collections.Generic;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Parsers
{
  public abstract class AbstractProxyListParser: IProxyListParser
  {
    /// <summary>
    /// Gets or sets the default protocol value (http, socks4, socks5, etc.).
    /// This is used when we know all the proxies has the same protocol which can't be determined from the data parsed.
    /// </summary>
    /// <remarks>May be <c>null</c>.</remarks>
    public string DefaultProtocol { get; set; }

    /// <summary>
    /// Gets or sets the default value for SSL support.
    /// This is used when we know all the proxies have the same settings which can't be determined from the data parsed.
    /// </summary>
    /// <remarks>May be <c>null</c>.</remarks>
    public bool? DefaultSSL { get; set; }

    /// <summary>
    /// Gets or sets the default value for Google support.
    /// This is used when we know all the proxies have the same settings which can't be determined from the data parsed.
    /// </summary>
    /// <remarks>May be <c>null</c>.</remarks>
    public bool? DefaultGoogle { get; set; }

    /// <inheritdoc />
    public abstract IEnumerable<string> Validate();

    /// <inheritdoc />
    public abstract IEnumerable<ProxyInformation> ParseContent(string content, Action<string> onError);
  }
}
