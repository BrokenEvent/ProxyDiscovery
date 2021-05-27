[![GitHub license](https://img.shields.io/badge/license-MIT-brightgreen.svg?style=flat-square)](https://raw.githubusercontent.com/BrokenEvent/ProxyDiscovery/master/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/BrokenEvent.ProxyDiscovery.svg?style=flat-square?color=informational&label=NuGet)](https://www.nuget.org/packages/BrokenEvent.ProxyDiscovery/)


# ProxyDiscovery
.NET library for valid and usable `IP:Port` proxy list discovery.

This project was created under inspiration of the [ProxySharp](https://github.com/m-henderson/ProxySharp) package and dedicated
to obtain lists of proxies from different locations, not only hardcoded ones, to give the user control of which exactly proxies
they want and to check if the proxies are really operational in real time.

## Features

* Customization of every step of getting proxy list: downloading from different sources with their own parsing rules.
* Proxy list filtering: SSL, Google support, port range, server location.
* Embedded templates for several well-known proxy list sources.
* Proxy availability testing for the result list to be not only a list of ip:port values, but list of something which is working right now.

## Usage

### Proxy list sources

Each website of another source of proxy list uses different text format which needs to be parsed in different way. The package is separated into
levels of service to control each step.

The lowest level is `IProxyListSource` which obtains the proxy list in raw text form by downloading from URL, loading a file and so on. `IProxyListParser` implementation
transform raw text data into list of `ProxyInformation` which is usable. These two things are combined in `CompositeProxyProvider`, implementation of `IProxyListProvider`.
For the proxy list sources which don't use "load-parse" chain, the user can use their own implementation.

### Embedded proxy list providers

Class `BrokenEvent.ProxyDiscovery.WellKnown` contains templates for some well-known proxy sources. You may use their parts or just `*ProxyProvider` members to get
list or proxies from one or another server.

You can use it just as simple:

```CSharp
foreach (ProxyInformation proxy in BrokenEvent.ProxyDiscovery.WellKnown.FreeProxyProvider.GetProxies())
{
  System.Console.WriteLine(proxy);
}
```

or use its TAP variant:

```CSharp
foreach (ProxyInformation proxy in await BrokenEvent.ProxyDiscovery.WellKnown.FreeProxyProvider.GetProxiesAsync(CancellationToken.None))
{
  Console.WriteLine(proxy);
}
```

This will get you list of `IP:Port` proxies.

### Custom proxy list sources

To create your own proxy list source you may combine proxy list acquisiting with parsing:

```CSharp
public const string FreeProxiesUrl = "https://free-proxy-list.net/";

public static readonly IProxyListSource FreeProxiesSource = new WebProxyListSource(FreeProxiesUrl);

public static readonly IProxyListParser FreeProxiesParser = new HtmlProxyListParser
{
  ProxyTablePath = "//table[@id='proxylisttable']/tbody/tr",
  IpPath = "td[1]",
  PortPath = "td[2]",
  IsHttpPath = "td[7]",
  CountryPath = "td[4]",
  GooglePassedPath = "td[6]"
};

public static readonly CompositeProxyProvider FreeProxyProvider = new CompositeProxyProvider(FreeProxiesSource, FreeProxiesParser);
```
(fragment of `WellKnown` class)

### Advanced example

To create an automated fully-featured proxy list downloader, you should use `ProxyDiscovery` class.

```CSharp
ProxyDiscovery discovery = new ProxyDiscovery
{
  Providers = // setup proxy providers, embedded or your own
  {
    WellKnown.FreeProxyProvider,
    WellKnown.PubProxyProvider
  },
  Filters = // setup filters to get only the proxies you want, optional
  { 
    new HttpsFilter()
  },
  Checker = // setup the proxy checker and the URL you want to use for check against, optional
    new ProxyChecker { TargetUrl = "https://brokenevent.com", Timeout = 1000 },
};

// callback is called when the proxy discovery determines state of a proxy - working or not
// you can use this because the update process with availability check may take up to several minutes (on long lists)
// but this callback will be called in real time during Update
discovery.ProxyCheckComplete += s =>
{
  if (s.Result == ProxyCheckResult.OK)
    Console.WriteLine(s.Proxy);
};

// update proxy list and perform all the necessary operations
await discovery.Update(CancellationToken.None);

// or instead of using ProxyCheckComplete you may just wait until the Update completes
foreach (ProxyState state in discovery.Proxies)
{
  Console.WriteLine(state.Proxy);
}
```

The project `BrokenEvent.ProxyDiscovery.Tester` contains complete usage example.
