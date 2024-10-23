using System.Collections.Generic;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Filters
{
  /// <summary>
  /// Filters proxy list by protocol.
  /// </summary>
  public sealed class ProtocolFilter: IProxyFilter
  {
    /// <summary>
    /// Gets or sets the required protocol name. All names are in lowercase.
    /// </summary>
    public string Protocol { get; set; }

    public IEnumerable<string> Validate()
    {
      if (string.IsNullOrWhiteSpace(Protocol))
        yield return "Protocol cannot be empty";
    }

    public bool DoesPassFilter(ProxyInformation proxy)
    {
      return proxy.Protocol == Protocol;
    }
  }
}
