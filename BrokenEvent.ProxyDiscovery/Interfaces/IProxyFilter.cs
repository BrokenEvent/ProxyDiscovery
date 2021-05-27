using System;

namespace BrokenEvent.ProxyDiscovery.Interfaces
{
  /// <summary>
  /// Performs static proxy filtering.
  /// </summary>
  public interface IProxyFilter: IValidatable
  {
    /// <summary>
    /// Checks whether the proxy passes tie filter.
    /// </summary>
    /// <param name="proxy">Proxy instance to check.</param>
    /// <returns><c>True</c> if the proxy passes the filter and should remain in list and <c>False</c> otherwise.</returns>
    bool DoesPassFilter(ProxyInformation proxy);
  }
}
