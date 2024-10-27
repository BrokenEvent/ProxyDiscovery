using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BrokenEvent.ProxyDiscovery.Interfaces
{
  /// <summary>
  /// Provides a proxy list.
  /// </summary>
  public interface IProxyListProvider: IValidatable
  {
    /// <summary>
    /// Gets the proxies enumeration asynchronously.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="onError">Call in case of proxy list acquisition errors.</param>
    /// <returns>Enumeration of proxies. In case of failure, can return <c>null</c>.</returns>
    Task<IEnumerable<ProxyInformation>> GetProxiesAsync(CancellationToken ct, Action<string> onError);
  }
}
