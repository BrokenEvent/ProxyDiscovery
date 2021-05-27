using System.Collections.Generic;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Filters
{
  /// <summary>
  /// Filter passing only HTTPS proxies.
  /// </summary>
  public sealed class HttpsFilter: IProxyFilter
  {
    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      // nothing to validate
      yield break;
    }

    /// <inheritdoc />
    public bool DoesPassFilter(ProxyInformation proxy)
    {
      return proxy.IsHttps.HasValue && proxy.IsHttps.Value;
    }
  }
}
