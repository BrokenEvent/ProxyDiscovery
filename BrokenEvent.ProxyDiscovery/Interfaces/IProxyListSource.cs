using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrokenEvent.ProxyDiscovery.Interfaces
{
  /// <summary>
  /// Represents the source of the proxy list.
  /// </summary>
  public interface IProxyListSource: IValidatable
  {
    /// <summary>
    /// Gets the raw proxy list content asynchronously.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="onError">Call in case of proxy list acquisition errors.</param>
    /// <returns>Proxy list string.</returns>
    Task<string> GetContentAsync(CancellationToken ct, Action<string> onError);
  }
}
