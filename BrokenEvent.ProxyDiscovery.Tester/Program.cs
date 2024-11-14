using System;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Interfaces;
using BrokenEvent.ProxyDiscovery.Parsers;

namespace BrokenEvent.ProxyDiscovery.Tester
{
  class Program
  {
    static void Main(string[] args)
    {
      ProxyDiscovery discovery = Test().GetAwaiter().GetResult();

      Console.WriteLine("Proxy check complete. Found: {0} items", discovery.Proxies.Count);
      Console.ReadLine();
    }

    private static async Task<ProxyDiscovery> Test()
    {
      ProxyDiscovery discovery = new ProxyDiscovery()
        .AddFileProxyListProvider(
          "proxies.csv",
          new CsvProxyListParser
          {
            IpColumn = 0,
            PortColumn = 1,
            CountryColumn = 2,
            IsSSLColumn = 3,
            DefaultProtocol = "http"
          }
        )
        .SSLOnly()
        .WithTunnelHeadCheck("https://brokenevent.com");
      
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

    private static void Discovery_AcquisitionComplete(IProxyListProvider proxyListProvider, int i)
    {
      Console.WriteLine($"Discovery proxy list acquired: {i} proxies from {proxyListProvider}");
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
