using System;

using BrokenEvent.ProxyDiscovery.Checkers;
using BrokenEvent.ProxyDiscovery.Filters;
using BrokenEvent.ProxyDiscovery.Interfaces;
using BrokenEvent.ProxyDiscovery.Providers;
using BrokenEvent.ProxyDiscovery.Sources;

namespace BrokenEvent.ProxyDiscovery
{
  public static class Fluent
  {
    /// <summary>
    /// Adds country filtering. Only countries listed will be included to the search results.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="countries">Countries enumeration in human-readable form.</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="IncludeCountryFilter"/>
    public static ProxyDiscovery IncludeCountries(this ProxyDiscovery discovery, string countries)
    {
      if (string.IsNullOrWhiteSpace(countries))
        throw new ArgumentNullException(nameof(countries));

      discovery.Filters.Add(new IncludeCountryFilter { Locations = countries });
      return discovery;
    }

    /// <summary>
    /// Adds country filtering. Countries listed will not be included to the search results.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="countries">Countries enumeration in human-readable form.</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="ExcludeCountryFilter"/>
    public static ProxyDiscovery ExcludeCountries(this ProxyDiscovery discovery, string countries)
    {
      if (string.IsNullOrWhiteSpace(countries))
        throw new ArgumentNullException(nameof(countries));

      discovery.Filters.Add(new ExcludeCountryFilter { Locations = countries });
      return discovery;
    }

    /// <summary>
    /// Adds SSL filtering. Only proxies which support HTTPS/SSL will be included to the search results.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="strictHttps">Whether to exclude from search results proxies with unknown HTTPS state.
    /// <c>true</c> to include only known HTTPS proxies, <c>false</c> to include also unknown.</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="HttpsFilter"/>
    /// <remarks>Since the <see cref="ProxyHttpConnectChecker"/> supports only HTTPS, if it used, <paramref name="strictHttps"/> may be set to <c>false</c> -
    /// proxy checker will check and will not pass non-HTTPS proxies. It will also update proxy HTTPS state.</remarks>
    public static ProxyDiscovery HttpsOnly(this ProxyDiscovery discovery, bool strictHttps = true)
    {
      discovery.Filters.Add(new HttpsFilter { AllowUnknown = false });
      return discovery;
    }

    /// <summary>
    /// Adds Google-passed filtering. Only proxies which are known to work with Google services will be included to search results.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="strictGoogle">Whether to exclude from search results proxies with unknown Google support state.
    /// <c>true</c> to include only known Google-supported proxies, <c>false</c> to include also unknown.</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="GooglePassedFilter"/>
    public static ProxyDiscovery GoogleOnly(this ProxyDiscovery discovery, bool strictGoogle = true)
    {
      discovery.Filters.Add(new GooglePassedFilter { AllowUnknown = !strictGoogle });
      return discovery;
    }

    /// <summary>
    /// Adds protocol-based filtering. Only proxies with given protocol value will be included to the search results.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="testUrl">Protocol name (http, socks4, socks5, etc.)</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="ProtocolFilter"/>
    public static ProxyDiscovery WithProtocol(this ProxyDiscovery discovery, string testUrl)
    {
      if (string.IsNullOrWhiteSpace(testUrl))
        throw new ArgumentNullException(nameof(testUrl));

      discovery.Filters.Add(new ProtocolFilter { Protocol = testUrl });
      return discovery;
    }

    /// <summary>
    /// Adds port-based filtering. Only proxies which pass the port rules will be added to the search results.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="ports">Ports filtering rules. See remarks.</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="PortFilter"/>
    /// <remarks>
    /// <para>Syntax uses commas and dashes. "80, 81" means two separate ports (<c>port == 80 || port == 81</c>).
    /// "80-90" means interval, exclusively (<c>port >= 80 &amp;&amp; port &lt;= 90</c>).
    /// Those can be mixed as "80, 90-100, 3128".</para>
    /// <para>When a value is prefixed with ~, it is counted as an exclude filter: "~80" (include all except 80), "~80-100" (include all except within range from
    ///  80 to 100), "80-100, ~91" (include all from 80 to 100, but not 91).</para>
    /// <para>No include filters means to allow everything (except the exclusions).</para>
    /// </remarks>
    public static ProxyDiscovery WithPorts(this ProxyDiscovery discovery, string ports)
    {
      if (string.IsNullOrWhiteSpace(ports))
        throw new ArgumentNullException(nameof(ports));

      discovery.Filters.Add(new PortFilter { FilterString = ports });
      return discovery;
    }

    /// <summary>
    /// Adds proxy check via HTTP CONNECT. Each proxy will be tested for creating tunnels via HTTP CONNECT method. The tunnel will not be tested.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="testUrl">Target URL to check with.</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="ProxyHttpConnectChecker"/>
    /// <seealso cref="NoneTunnelTester"/>
    public static ProxyDiscovery CheckHttpProxy(this ProxyDiscovery discovery, string testUrl)
    {
      if (string.IsNullOrWhiteSpace(testUrl))
        throw new ArgumentNullException(nameof(testUrl));

      discovery.Checker = new ProxyHttpConnectChecker
      {
        TargetUrl = new Uri(testUrl),
        TunnelTester = new NoneTunnelTester()
      };
      return discovery;
    }

    /// <summary>
    /// Adds proxy check via HTTP CONNECT. Each proxy will be tester for creating tunnels via HTTP CONNECT method.
    /// The tunnel created will be tested via HTTP HEAD method for the resource specified in <see cref="testUrl"/>.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="testUrl">Target URL to check with.</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="ProxyHttpConnectChecker"/>
    /// <seealso cref="HttpHeadTunnelTester"/>
    public static ProxyDiscovery CheckHttpProxyHead(this ProxyDiscovery discovery, string testUrl)
    {
      if (string.IsNullOrWhiteSpace(testUrl))
        throw new ArgumentNullException(nameof(testUrl));

      discovery.Checker = new ProxyHttpConnectChecker
      {
        TargetUrl = new Uri(testUrl),
        TunnelTester = new HttpHeadTunnelTester()
      };
      return discovery;
    }

    /// <summary>
    /// Adds a web-based proxy list provider.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="sourceUrl">URL to download proxy list.</param>
    /// <param name="parser">Parser to convert raw content downloaded to enumeration of <see cref="ProxyInformation"/>.</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="CompositeProxyProvider"/>
    /// <seealso cref="WebProxyListSource"/>
    public static ProxyDiscovery AddWebProxyListProvider(this ProxyDiscovery discovery, string sourceUrl, IProxyListParser parser)
    {
      if (parser == null)
        throw new ArgumentNullException(nameof(parser));

      if (string.IsNullOrWhiteSpace(sourceUrl))
        throw new ArgumentNullException(nameof(sourceUrl));

      discovery.Providers.Add(
          new CompositeProxyProvider(
              new WebProxyListSource(sourceUrl),
              parser
            )
        );
      return discovery;
    }

    /// <summary>
    /// Adds a file-based proxy list provider.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="filePath">Absolute or relative path to file to load proxy list.</param>
    /// <param name="parser">Parser to convert raw content loaded from file to enumeration of <see cref="ProxyInformation"/>.</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="CompositeProxyProvider"/>
    /// <seealso cref="WebProxyListSource"/>
    public static ProxyDiscovery AddFileProxyListProvider(this ProxyDiscovery discovery, string filePath, IProxyListParser parser)
    {
      if (parser == null)
        throw new ArgumentNullException(nameof(parser));

      if (string.IsNullOrWhiteSpace(filePath))
        throw new ArgumentNullException(nameof(filePath));

      discovery.Providers.Add(
          new CompositeProxyProvider(
              new FileProxyListSource(filePath), 
              parser
            )
        );
      return discovery;
    }
  }
}
