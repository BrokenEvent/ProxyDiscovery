using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Filters;
using BrokenEvent.ProxyDiscovery.Providers;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class ProxyDiscoveryTests
  {
   
    private StaticProxyProvider testProvider = new StaticProxyProvider
    {
      /*0*/ new ProxyInformation("192.168.0.1", 80, "http", true, true, country: "test"),  // HTTPS, Google
      /*1*/ new ProxyInformation("192.168.0.2", 180, "http", false, true, country: "test"),  // not HTTPS, Google
      /*2*/ new ProxyInformation("192.168.0.3", 280, "http", true, false, country: "test"),  // HTTPS, not Google
      /*3*/ new ProxyInformation("192.168.0.4", 380, "http", null, true, country: "test"),  // n/a, not Google
      /*4*/ new ProxyInformation("192.168.0.5", 480, "http", true, null, country: "test"),  // HTTPS, n/a
      /*5*/ new ProxyInformation("192.168.0.1", 580, "http", true, true, country: "debug"),
    };

    private IEnumerable<ProxyState> BuildStates(params int[] indexes)
    {
      for (int i = 0; i < indexes.Length; i++)
        yield return new ProxyState(testProvider.Proxies[indexes[i]], ProxyCheckResult.Unchecked, "test", TimeSpan.Zero);
    }

    [Test]
    public async Task Unfiltered()
    {
      ProxyDiscovery discovery = new ProxyDiscovery
      {
        Providers = { testProvider },
        MaxThreads = 1
      };

      await discovery.Update(CancellationToken.None);

      CollectionAssert.AreEquivalent(BuildStates(0, 1, 2, 3, 4, 5), discovery.Proxies);
    }

    [Test]
    public async Task HttpsOnly()
    {
      ProxyDiscovery discovery = new ProxyDiscovery
      {
        Providers = { testProvider },
        Filters = { new SSLFilter() },
        MaxThreads = 1
      };

      await discovery.Update(CancellationToken.None);

      CollectionAssert.AreEquivalent(BuildStates(0, 2, 4, 5), discovery.Proxies);
    }

    [Test]
    public async Task GoogleOnly()
    {
      ProxyDiscovery discovery = new ProxyDiscovery()
      {
        Providers = { testProvider },
        Filters = { new GooglePassedFilter() },
        MaxThreads = 1
      };

      await discovery.Update(CancellationToken.None);

      CollectionAssert.AreEquivalent(BuildStates(0, 1, 3, 5), discovery.Proxies);
    }

    [Test]
    public async Task PortsAndHttps()
    {
      ProxyDiscovery discovery = new ProxyDiscovery
      {
        Providers = { testProvider },
        Filters = { new SSLFilter(), new PortFilter { FilterString = "50-300, 500-1000" } },
        MaxThreads = 1
      };

      await discovery.Update(CancellationToken.None);

      CollectionAssert.AreEquivalent(BuildStates(0, 2, 5), discovery.Proxies);
    }

    [Test]
    public async Task HttpsAndLocation()
    {
      ProxyDiscovery discovery = new ProxyDiscovery
      {
        Providers = { testProvider },
        Filters = { new SSLFilter(), new IncludeCountryFilter { Countries = "test" } },
        MaxThreads = 1
      };

      await discovery.Update(CancellationToken.None);

      CollectionAssert.AreEquivalent(BuildStates(0, 2, 4), discovery.Proxies);
    }
  }
}
