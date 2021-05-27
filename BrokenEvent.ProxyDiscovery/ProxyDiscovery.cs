using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Helpers;
using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery
{
  /// <summary>
  /// Performs proxy list update and availability checks.
  /// </summary>
  public sealed class ProxyDiscovery
  {
    private List<ProxyState> proxies;

    /// <summary>
    /// Gets the list of proxy list providers.
    /// </summary>
    /// <remarks>If several proxy list providers return the same proxies, they will be merged.</remarks>
    public List<IProxyListProvider> Providers { get; } = new List<IProxyListProvider>();

    /// <summary>
    /// Gets the list of proxy static filters.
    /// </summary>
    public List<IProxyFilter> Filters { get; } = new List<IProxyFilter>();

    /// <summary>
    /// Gets or sets the proxy availability checker. May be <c>null</c> to disable the check (faster).
    /// </summary>
    public IProxyChecker Checker { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of threads used to check proxies (used if <see cref="Checker"/> is not <c>null</c>).
    /// </summary>
    public int MaxThreads { get; set; } = 32;

    /// <summary>
    /// Gets the list of proxies obtained during the recent <see cref="Update"/> operation.
    /// </summary>
    public IReadOnlyList<ProxyState> Proxies
    {
      get { return proxies; }
    }

    /// <summary>
    /// Gets a single proxy and removes it from list.
    /// </summary>
    /// <returns>Proxy information or <c>null</c> if the list is empty.</returns>
    public ProxyInformation PopProxy()
    {
      if (proxies.Count == 0)
        return null;

      ProxyState state = proxies[0];
      proxies.RemoveAt(0);

      return state.Proxy;
    }

    /// <summary>
    /// Updates the proxy discovery and gets fresh proxies list.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="maxResults">Maximum number of working proxies to return. Default is 0 which means no limits.</param>
    public async Task Update(CancellationToken ct, int maxResults = 0)
    {
      ProxyDiscoveryUpdate update = new ProxyDiscoveryUpdate(this, maxResults);

      // get the proxies lists in unified list
      foreach (IProxyListProvider provider in Providers)
      {
        if (LogMessage != null)
          LogMessage($"Acquiring proxies list: {provider}");

        try
        {
          update.UpdateProxyList(await provider.GetProxiesAsync(ct).ConfigureAwait(false));
        }
        catch (Exception e)
        {
          if (LogMessage != null)
            LogMessage($"Failed to get proxy list from: {provider}. Message: {e.Message}");
        }
      }

      if (LogMessage != null)
        LogMessage($"Proxies acquired: {update.Count}");

      // filter list (if any filters)
      if (update.HasFilters)
      {
        await Task.Run((Action)update.FilterProxies, ct).ConfigureAwait(false);
        if (LogMessage != null)
          LogMessage($"Proxies remain after filtering: {update.Count}");
      }

      // check availability
      if (Checker != null)
      {
        if (LogMessage != null)
          LogMessage($"Checking proxy list availability, maxThreads = {MaxThreads}, maxResults = {(maxResults == 0 ? "unlimited" : maxResults.ToString())}");

        Checker.Prepare();

        await new TaskQueue<ProxyInformation>(update.Proxies, update.CheckProxyAvailability, MaxThreads, ct).Run().ConfigureAwait(false);
      }
      else
        // or not
        update.CollectResultsUnchecked();

      // save the results
      proxies = update.Results;
    }

    private void OnProxyCheckComplete(ProxyState state)
    {
      if (ProxyCheckComplete != null)
        ProxyCheckComplete(state);
    }

    /// <summary>
    /// Event is called when the proxy discovery writes a verbose log message.
    /// </summary>
    public event Action<string> LogMessage;

    /// <summary>
    /// Event is called when the availability check is completed for a proxy during <see cref="Update"/>.
    /// </summary>
    /// <remarks>
    /// <para>Event is never called when <see cref="IProxyChecker"/> is <c>null</c>.</para>
    /// <para>Called from the background thread.</para>
    /// </remarks>
    public event Action<ProxyState> ProxyCheckComplete;

    private class ProxyDiscoveryUpdate
    {
      private readonly ProxyDiscovery host;
      private readonly HashSet<ProxyInformation> proxySet = new HashSet<ProxyInformation>();
      private readonly List<IProxyFilter> filters;
      private readonly IProxyChecker checker;
      private readonly int maxResults;

      private SpinLock proxyListLock = new SpinLock();
      private int resultsCount;
      private List<ProxyState> results = new List<ProxyState>();

      public ProxyDiscoveryUpdate(ProxyDiscovery host, int maxResults)
      {
        this.host = host;
        filters = new List<IProxyFilter>(host.Filters);
        checker = host.Checker;
        this.maxResults = maxResults;
      }

      public void UpdateProxyList(IEnumerable<ProxyInformation> proxies)
      {
        proxySet.UnionWith(proxies);
      }

      public IEnumerable<ProxyInformation> Proxies
      {
        get { return proxySet; }
      }

      public int Count
      {
        get { return proxySet.Count; }
      }

      public bool HasFilters
      {
        get { return filters.Count > 0; }
      }

      public void FilterProxies()
      {
        proxySet.RemoveWhere(CheckIfRemoveProxy);
      }

      private bool CheckIfRemoveProxy(ProxyInformation proxy)
      {
        foreach (IProxyFilter filter in filters)
        {
          if (!filter.DoesPassFilter(proxy))
            return true;
        }

        return false;
      }

      public async Task<bool> CheckProxyAvailability(ProxyInformation proxy, CancellationToken ct)
      {
        if (maxResults > 0 && resultsCount >= maxResults)
          return false;

        ProxyState state = await checker.CheckProxy(proxy, ct).ConfigureAwait(false);

        if (state.Result == ProxyCheckResult.OK && !AddResult(state))
          return false;

        host.OnProxyCheckComplete(state);
        return true;
      }

      private bool AddResult(ProxyState state)
      {
        bool lockTaken = false;
        try
        {
          proxyListLock.Enter(ref lockTaken);

          if (maxResults > 0 && resultsCount >= maxResults)
            return false;

          results.Add(state);
          resultsCount++;

          return true;
        }
        finally
        {
          if (lockTaken)
            proxyListLock.Exit(false);
        }
      }

      public List<ProxyState> Results
      {
        get { return results; }
      }

      public void CollectResultsUnchecked()
      {
        foreach (ProxyInformation proxy in proxySet)
        {
          results.Add(new ProxyState(proxy, ProxyCheckResult.Unckeched, "Not checked", TimeSpan.Zero));

          if (maxResults > 0 && results.Count >= maxResults)
            break;
        }
      }
    }
  }
}
