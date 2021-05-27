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
    /// <returns>Proxy enumeration.</returns>
    IEnumerable<ProxyInformation> ParseContent(string content);
  }
}
