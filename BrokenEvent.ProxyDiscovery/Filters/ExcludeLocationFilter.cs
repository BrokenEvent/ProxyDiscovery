using System;

namespace BrokenEvent.ProxyDiscovery.Filters
{
  /// <summary>
  /// Filters proxies by location exclude list.
  /// </summary>
  public sealed class ExcludeLocationFilter: LocationBaseFilter
  {
    public override bool DoesPassFilter(ProxyInformation proxy)
    {
      if (string.IsNullOrWhiteSpace(proxy.Country))
        return true;

      foreach (string item in LocationItems)
        if (item.Equals(proxy.Country, StringComparison.InvariantCultureIgnoreCase))
          return false;

      return true;
    }
  }
}
