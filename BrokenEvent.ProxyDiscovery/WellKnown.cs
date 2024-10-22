using BrokenEvent.ProxyDiscovery.Interfaces;
using BrokenEvent.ProxyDiscovery.Parsers;
using BrokenEvent.ProxyDiscovery.Providers;
using BrokenEvent.ProxyDiscovery.Sources;

namespace BrokenEvent.ProxyDiscovery
{
  /// <summary>
  /// Storage of settings of the well-known proxy sources.
  /// </summary>
  public static class WellKnown
  {
    public const string FreeProxiesUrl = "https://free-proxy-list.net/";
    public const string USProxiesUrl = "https://www.us-proxy.org/";
    public const string UKProxiesUrl = "https://free-proxy-list.net/uk-proxy.html";
    public const string SSLProxiesUrl = "https://www.sslproxies.org/";
    public const string AnonymousProxiesUrl = "https://free-proxy-list.net/anonymous-proxy.html";

    /// <summary>
    /// Web source for free-proxy-list.net
    /// </summary>
    public static readonly IProxyListSource FreeProxiesSource = new WebProxyListSource(FreeProxiesUrl);

    /// <summary>
    /// Web source for www.us-proxy.org
    /// </summary>
    public static readonly IProxyListSource USProxiesSource = new WebProxyListSource(USProxiesUrl);

    /// <summary>
    /// Web source for free-proxy-list.net/uk-proxy.html
    /// </summary>
    public static readonly IProxyListSource UKProxiesSource = new WebProxyListSource(UKProxiesUrl);

    /// <summary>
    /// Proxy list parser for free-proxy-list.net web site and others with the same page structure.
    /// </summary>
    public static readonly IProxyListParser FreeProxiesParser = new HtmlProxyListParser()
    {
      ProxyTablePath = "//section[@id='list']//table/tbody/tr",
      IpPath = "td[1]",
      PortPath = "td[2]",
      IsHttpPath = "td[7]",
      CountryPath = "td[4]",
      GooglePassedPath = "td[6]"
    };

    /// <summary>
    /// Proxy provider for free-proxy-list.net
    /// </summary>
    public static readonly CompositeProxyProvider FreeProxyProvider = new CompositeProxyProvider(FreeProxiesSource, FreeProxiesParser);

    /// <summary>
    /// Proxy provider for www.us-proxy.org
    /// </summary>
    public static readonly CompositeProxyProvider USProxyProvider = new CompositeProxyProvider(USProxiesSource, FreeProxiesParser);

    /// <summary>
    /// Proxy provider for free-proxy-list.net/uk-proxy.html
    /// </summary>
    public static readonly CompositeProxyProvider UKProxyProvider = new CompositeProxyProvider(UKProxiesSource, FreeProxiesParser);

    /// <summary>
    /// Proxy provider for PubProxy. GitHub repo (https://github.com/clarketm/proxy-list). Public real-time API: http://pubproxy.com/
    /// </summary>
    public static readonly CompositeProxyProvider PubProxyProvider = new CompositeProxyProvider(
        new WebProxyListSource("https://raw.githubusercontent.com/clarketm/proxy-list/master/proxy-list.txt"),
        new LineRegexProxyListParser(@"(?<address>[\d\.]+):(?<port>\d+)\s+(?<location>\w{2})-(\w{1})(-(?<https>S{1}))?!{0,1}\s(?<google>(?:\+|-))\s*$")
      );
  }
}
