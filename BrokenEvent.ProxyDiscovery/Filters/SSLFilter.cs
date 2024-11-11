using System.Collections.Generic;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Filters
{
  /// <summary>
  /// Filter passing only HTTPS proxies.
  /// </summary>
  public sealed class SSLFilter: IProxyFilter
  {
    /// <summary>
    /// Gets or sets the value indicating whether to pass proxies with unknown <see cref="ProxyInformation.IsSSL"/> value.
    /// </summary>
    /// <remarks>If this value is <c>false</c> (default), only proxies which are known to support HTTPS will pass the filter.</remarks>
    public bool AllowUnknown { get; set; }

    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      // nothing to validate
      yield break;
    }

    /// <inheritdoc />
    public bool DoesPassFilter(ProxyInformation proxy)
    {
      return proxy.IsSSL.HasValue ? proxy.IsSSL.Value : AllowUnknown;
    }
  }
}
