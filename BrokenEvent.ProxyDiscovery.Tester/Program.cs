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

      await discovery.Update(CancellationToken.None, 10);
      return discovery;
    }

    private static void Discovery_ProxyCheckComplete(ProxyState state)
    {
      Console.WriteLine($"{state.Proxy.ToAddress()} ({state.Proxy.Location}): {state.Result} - {state.Status} - {state.Delay.TotalMilliseconds}ms");
    }
  }
}
