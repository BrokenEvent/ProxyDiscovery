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
    private ProxyDiscoveryStatus status;
    private bool updateLock;

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
    /// <remarks>Will remain <c>null</c> until the proxy check is completed.</remarks>
    public IReadOnlyList<ProxyState> Proxies
    {
      get { return proxies; }
    }

    /// <summary>
    /// Gets the value indicating whether to randomize proxy list.
    /// </summary>
    /// <remarks>
    /// <para>The randomization takes place after filtering and before proxy checking.</para>
    /// <para>The option is useful if there are several proxy sources. By default they will be checked in order they appear,
    /// but this allows us to get proxies from different lists checked first.</para>
    /// </remarks>
    public bool RandomizeList { get; set; }

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
    /// Gets the proxy discovery status.
    /// </summary>
    public ProxyDiscoveryStatus Status
    {
      get { return status; }
      private set
      {
        status = value;
        if (StatusChanged != null)
          StatusChanged(value);
      }
    }

    /// <summary>
    /// Updates the proxy discovery and gets fresh proxies list.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="maxResults">Maximum number of working proxies to return. Default is 0 which means no limits.</param>
    /// <remarks>You can track the state by <see cref="Status"/> property and <see cref="StatusChanged"/>, <see cref="AcquisitionComplete"/> and
    /// <see cref="FilteringComplete"/> events.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="Update"/> is called several times at once.</exception>
    public async Task Update(CancellationToken ct, int maxResults = 0)
    {
      // don't call update several times
      if (updateLock)
        throw new InvalidOperationException("Duplicate call of Update");

      // set the lock
      updateLock = true;

      try
      {
        ProxyDiscoveryUpdater updater = new ProxyDiscoveryUpdater(this, maxResults);

        // clear the previous results if any
        proxies = null;

        // update status
        Status = ProxyDiscoveryStatus.Acuisition;

        // get the proxies lists in unified list
        foreach (IProxyListProvider provider in Providers)
        {
          if (LogMessage != null)
            LogMessage($"Acquiring proxies list: {provider}");

          try
          {
            updater.UpdateProxyList(await provider.GetProxiesAsync(ct).ConfigureAwait(false));
          }
          catch (Exception e)
          {
            if (LogMessage != null)
              LogMessage($"Failed to get proxy list from: {provider}. Message: {e.Message}");
          }
        }

        if (LogMessage != null)
          LogMessage($"Proxies acquired: {updater.Count}");

        // status notify
        if (AcquisitionComplete != null)
          AcquisitionComplete(updater.Count);

        // filter list (if any filters)
        if (updater.HasFilters)
        {
          // update status
          Status = ProxyDiscoveryStatus.Filtering;

          await Task.Run((Action)updater.FilterProxies, ct).ConfigureAwait(false);

          if (LogMessage != null)
            LogMessage($"Proxies remain after filtering: {updater.Count}");
        }

        // status notify
        if (FilteringComplete != null)
          FilteringComplete(updater.Count);

        // generate proxies list base on proxies set with optional randomization
        updater.SetProxiesList(RandomizeList);

        // check availability
        if (Checker != null)
        {
          // update status
          Status = ProxyDiscoveryStatus.Checking;

          if (LogMessage != null)
            LogMessage($"Checking proxy list availability, maxThreads = {MaxThreads}, maxResults = {(maxResults == 0 ? "unlimited" : maxResults.ToString())}");

          Checker.Prepare();

          await new TaskQueue<ProxyInformation>(
                updater.Proxies,
                updater.CheckProxyAvailability,
                MaxThreads,
                ct
              ).Run()
            .ConfigureAwait(false);
        }
        else
          // or not
          updater.CollectResultsUnchecked();

        // save the results
        proxies = updater.Results;
      }
      finally
      {
        // release the lock at all costs
        updateLock = false;

        // and reset the state
        Status = ProxyDiscoveryStatus.Idle;
      }
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
    /// The parameter is the proxy just checked.
    /// </summary>
    /// <remarks>
    /// <para>Event is never called when <see cref="IProxyChecker"/> is <c>null</c>.</para>
    /// <para>Called from the background thread.</para>
    /// </remarks>
    public event Action<ProxyState> ProxyCheckComplete;

    /// <summary>
    /// Event is called when all proxy lists are acquired. The parameter is total count of proxies obtained.
    /// </summary>
    public event Action<int> AcquisitionComplete;

    /// <summary>
    /// Event is called when proxy filtering is completed. The parameter is count of proxies remain after filtering.
    /// </summary>
    /// <remarks>If there is no any filters, the event will still be fired.</remarks>
    public event Action<int> FilteringComplete;

    /// <summary>
    /// Event is called when <see cref="Status"/> is changed. The parameter is new status.
    /// </summary>
    public event Action<ProxyDiscoveryStatus> StatusChanged;

    private class ProxyDiscoveryUpdater
    {
      private readonly ProxyDiscovery host;
      private readonly HashSet<ProxyInformation> proxySet = new HashSet<ProxyInformation>();
      private readonly List<IProxyFilter> filters;
      private readonly IProxyChecker checker;
      private readonly int maxResults;

      private SpinLock proxyListLock = new SpinLock();
      private List<ProxyInformation> proxiesList = new List<ProxyInformation>();
      private int resultsCount;
      private List<ProxyState> results = new List<ProxyState>();

      public ProxyDiscoveryUpdater(ProxyDiscovery host, int maxResults)
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

      public void SetProxiesList(bool randomize)
      {
        proxiesList = new List<ProxyInformation>(proxySet);
        if (randomize)
          proxiesList.Randomize();
      }

      public IEnumerable<ProxyInformation> Proxies
      {
        get { return proxiesList; }
      }

      public List<ProxyState> Results
      {
        get { return results; }
      }

      public void CollectResultsUnchecked()
      {
        foreach (ProxyInformation proxy in proxiesList)
        {
          results.Add(new ProxyState(proxy, ProxyCheckResult.Unckeched, "Not checked", TimeSpan.Zero));

          if (maxResults > 0 && results.Count >= maxResults)
            break;
        }
      }
    }
  }

  public enum ProxyDiscoveryStatus
  {
    /// <summary>
    /// The proxy discovery is idle.
    /// </summary>
    Idle,
    /// <summary>
    /// The proxy discovery is acquiring and processing the proxy lists.
    /// </summary>
    Acuisition,
    /// <summary>
    /// The proxy discovery is filtering the proxy list.
    /// </summary>
    Filtering,
    /// <summary>
    /// The proxy discovery is checking proxies from the list.
    /// </summary>
    Checking
  }
}
