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
    /// Gets the proxies enumeration synchronously.
    /// </summary>
    /// <returns>Enumeration of proxies.</returns>
    IEnumerable<ProxyInformation> GetProxies();

    /// <summary>
    /// Gets the proxies enumeration asynchronously.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Enumeration of proxies.</returns>
    Task<IEnumerable<ProxyInformation>> GetProxiesAsync(CancellationToken ct);
  }
}
