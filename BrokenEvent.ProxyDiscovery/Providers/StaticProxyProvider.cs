using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Providers
{
  /// <summary>
  /// Provides static proxy list.
  /// </summary>
  public class StaticProxyProvider: IProxyListProvider, IEnumerable<ProxyInformation>
  {
    /// <summary>
    /// Static list of proxies.
    /// </summary>
    public List<ProxyInformation> Proxies { get; } = new List<ProxyInformation>();

    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      if (Proxies.Count == 0)
        yield return "Static proxy list cannot be empty";
    }

    /// <inheritdoc />
    public Task<IEnumerable<ProxyInformation>> GetProxiesAsync(CancellationToken ct, Action<string> onError)
    {
      return Task.FromResult((IEnumerable<ProxyInformation>)Proxies);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public IEnumerator<ProxyInformation> GetEnumerator()
    {
      return Proxies.GetEnumerator();
    }

    /// <summary>
    /// Adds a proxy to <see cref="Proxies"/> list.
    /// </summary>
    /// <param name="proxy">Proxy to add.</param>
    public void Add(ProxyInformation proxy)
    {
      Proxies.Add(proxy);
    }

    public override string ToString()
    {
      return "Static proxy list provider";
    }
  }
}
