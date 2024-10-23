using System;

namespace BrokenEvent.ProxyDiscovery.Filters
{
  /// <summary>
  /// Filters proxies by location include list.
  /// </summary>
  public sealed class IncludeLocationFilter: LocationBaseFilter
  {
    public override bool DoesPassFilter(ProxyInformation proxy)
    {
      if (string.IsNullOrWhiteSpace(proxy.Country))
        return false;

      foreach (string item in LocationItems)
        if (item.Equals(proxy.Country, StringComparison.InvariantCultureIgnoreCase))
          return true;

      return false;
    }
  }
}
