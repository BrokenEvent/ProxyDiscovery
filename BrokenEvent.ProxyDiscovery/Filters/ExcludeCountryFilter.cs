using System;

namespace BrokenEvent.ProxyDiscovery.Filters
{
  /// <summary>
  /// Filters proxies by country exclude list.
  /// </summary>
  public sealed class ExcludeCountryFilter: BaseCountryFilter
  {
    public override bool DoesPassFilter(ProxyInformation proxy)
    {
      if (string.IsNullOrWhiteSpace(proxy.Country))
        return true;

      foreach (string item in CountriesItems)
        if (item.Equals(proxy.Country, StringComparison.InvariantCultureIgnoreCase))
          return false;

      return true;
    }
  }
}
