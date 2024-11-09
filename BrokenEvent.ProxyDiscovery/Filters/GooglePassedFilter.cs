using System.Collections.Generic;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Filters
{
  /// <summary>
  /// Filter passing only Google-supported proxies.
  /// </summary>
  public sealed class GooglePassedFilter: IProxyFilter
  {
    /// <summary>
    /// Gets or sets the value indicating whether to pass proxies with unknown <see cref="ProxyInformation.IsGooglePassed"/> value.
    /// </summary>
    /// <remarks>If this value is <c>false</c> (default), only proxies which are known to be supported by Google will pass the filter.</remarks>
    public bool AllowUnknown { get; set; }

    public IEnumerable<string> Validate()
    {
      // nothing to validate
      yield break;
    }

    public bool DoesPassFilter(ProxyInformation proxy)
    {
      return proxy.IsGooglePassed.HasValue ? proxy.IsGooglePassed.Value : AllowUnknown;
    }
  }
}
