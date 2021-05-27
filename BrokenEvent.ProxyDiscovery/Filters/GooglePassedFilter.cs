using System.Collections.Generic;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Filters
{
  /// <summary>
  /// Filter passing only Google-supported proxies.
  /// </summary>
  public sealed class GooglePassedFilter: IProxyFilter
  {
    public IEnumerable<string> Validate()
    {
      // nothing to validate
      yield break;
    }

    public bool DoesPassFilter(ProxyInformation proxy)
    {
      return proxy.GooglePassed.HasValue && proxy.GooglePassed.Value;
    }
  }
}
