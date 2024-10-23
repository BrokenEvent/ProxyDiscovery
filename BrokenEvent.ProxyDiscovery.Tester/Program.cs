using System;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Checkers;
using BrokenEvent.ProxyDiscovery.Filters;

namespace BrokenEvent.ProxyDiscovery.Tester
{
  class Program
  {
    static void Main(string[] args)
    {
      ProxyDiscovery discovery = Test().GetAwaiter().GetResult();
      Console.ReadLine();
    }

    private static async Task<ProxyDiscovery> Test()
    {
      ProxyDiscovery discovery = new ProxyDiscovery
      {
        Providers =
        {
          WellKnown.FreeProxyProvider,
          WellKnown.PubProxyProvider
        },
        Filters = { new HttpsFilter() },
        Checker = new ProxyChecker { TargetUrl = "https://brokenevent.com", Timeout = 1000 },
      };
      discovery.LogMessage += Console.WriteLine;
      discovery.ProxyCheckComplete += Discovery_ProxyCheckComplete;
      discovery.FilteringComplete += Discovery_FilteringComplete;
      discovery.AcquisitionComplete += Discovery_AcquisitionComplete;
      discovery.StatusChanged += Discovery_StatusChanged;

      await discovery.Update(CancellationToken.None, 10);
      return discovery;
    }

    private static void Discovery_StatusChanged(ProxyDiscoveryStatus status)
    {
      Console.WriteLine($"Discovery status changed: {status}");
    }

    private static void Discovery_AcquisitionComplete(int proxies)
    {
      Console.WriteLine($"Discovery proxy lists acquired: {proxies} proxies");
    }

    private static void Discovery_FilteringComplete(int proxies)
    {
      Console.WriteLine($"Discovery proxy lists filtered: {proxies} remains");
    }

    private static void Discovery_ProxyCheckComplete(ProxyState state)
    {
      Console.WriteLine($"{state.Proxy.ToAddress()} ({state.Proxy.Country}): {state.Result} - {state.Status} - {state.Delay.TotalMilliseconds}ms");
    }
  }
}
