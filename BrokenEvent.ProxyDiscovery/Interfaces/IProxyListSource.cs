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
    /// Gets the value indicating whether the content has several pages.
    /// </summary>
    bool HasPages { get; }

    /// <summary>
    /// Gets the raw proxy list content asynchronously.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="pageNumber">Number of pages starting from 1.</param>
    /// <param name="onError">Call in case of proxy list acquisition errors.</param>
    /// <returns>Proxy list as string.</returns>
    /// <remarks>
    /// <para><paramref name="pageNumber"/> depends on <see cref="HasPages"/> value. </para>
    /// <para>If the property returns <c>false</c>, this method will be called once with <paramref name="pageNumber"/> 0.</para>
    /// <para>If the property returns <c>true</c>, this method will be called multiple times until it returns the empty content
    /// or no proxies could be parsed from the content returned. Each time <paramref name="pageNumber"/> will be incremented. For
    /// the first time, its value will be 1.</para>
    /// </remarks>
    Task<string> GetContentAsync(CancellationToken ct, int pageNumber, Action<string> onError);
  }
}
