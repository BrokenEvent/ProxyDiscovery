using System.Threading;
using System.Threading.Tasks;

namespace BrokenEvent.ProxyDiscovery.Interfaces
{
  /// <summary>
  /// Checks a proxy availability.
  /// </summary>
  public interface IProxyChecker: IValidatable
  {
    /// <summary>
    /// Prepares the proxy checker. Must be called before <see cref="CheckProxy"/> operations.
    /// </summary>
    void Prepare();

    /// <summary>
    /// Checks if the given proxy is available.
    /// </summary>
    /// <param name="proxy">Proxy to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of proxy availability check.</returns>
    Task<ProxyState> CheckProxy(ProxyInformation proxy, CancellationToken ct);
  }
}
