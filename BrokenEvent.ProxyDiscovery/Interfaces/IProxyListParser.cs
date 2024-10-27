using System;
using System.Collections.Generic;

namespace BrokenEvent.ProxyDiscovery.Interfaces
{
  /// <summary>
  /// Represents the parser for the proxy list text.
  /// </summary>
  public interface IProxyListParser: IValidatable
  {
    /// <summary>
    /// Parses the input content to the list of <see cref="ProxyInformation"/> instances.
    /// </summary>
    /// <param name="content">Text content to parse.</param>
    /// <param name="onError">Call in case of proxy list acquisition errors.</param>
    /// <returns>Proxy enumeration. In case of fatal error, may return <c>null</c>.</returns>
    IEnumerable<ProxyInformation> ParseContent(string content, Action<string> onError);
  }
}
