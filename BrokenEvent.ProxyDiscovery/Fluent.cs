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

      discovery.Filters.Add(new IncludeCountryFilter { Countries = countries });
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

      discovery.Filters.Add(new ExcludeCountryFilter { Countries = countries });
      return discovery;
    }

    /// <summary>
    /// Adds SSL filtering. Only proxies which support SSL will be included to the search results.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="strictHttps">Whether to exclude from search results proxies with unknown SSL state.
    /// <c>true</c> to include only known SSL proxies, <c>false</c> to include also unknown.</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="SSLFilter"/>
    /// <remarks>This is only useful when we work with HTTP proxies as SOCKS4/5 are tunnel-oriented and hence support SSL by default.</remarks>
    public static ProxyDiscovery SSLOnly(this ProxyDiscovery discovery, bool strictHttps = true)
    {
      discovery.Filters.Add(new SSLFilter { AllowUnknown = false });
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
    /// <param name="protocol">Protocol name in lowercase (http, socks4, socks5, etc.)</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="ProtocolFilter"/>
    public static ProxyDiscovery WithProtocol(this ProxyDiscovery discovery, string protocol)
    {
      if (string.IsNullOrWhiteSpace(protocol))
        throw new ArgumentNullException(nameof(protocol));

      discovery.Filters.Add(new ProtocolFilter { Protocol = protocol });
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
    /// Adds proxy connection check. Each proxy found will be tested by connecting to the target host through it. The tunnel created will not be tested.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="testUrl">Target URL to check with. Only http and https URL schemes are supported by the current implementation.
    /// Only scheme, domain and port parts will be used.</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="ProxyChecker"/>
    /// <seealso cref="NoneTunnelTester"/>
    /// <remarks>Current implementation supports HTTP (only with CONNECT HTTP method), SOCKS4/4A and SOCKS5 proxy protocols.</remarks>
    public static ProxyDiscovery WithConnectionCheck(this ProxyDiscovery discovery, string testUrl)
    {
      if (string.IsNullOrWhiteSpace(testUrl))
        throw new ArgumentNullException(nameof(testUrl));

      discovery.Checker = CreateDefaultProxyChecker(testUrl, new NoneTunnelTester());
      return discovery;
    }

    /// <summary>
    /// Adds detailed proxy connection check. Each proxy found will be tested by connecting to the target host through it.
    /// The created tunnel will be tested by sending HTTP HEAD request to the target server and processing the response to
    /// determine if the proxy really can perform the connection.
    /// </summary>
    /// <param name="discovery">Proxy discovery.</param>
    /// <param name="testUrl">Target URL to check with. Only http and https URL schemes are supported by the current implementation.</param>
    /// <returns>Proxy discovery.</returns>
    /// <seealso cref="ProxyChecker"/>
    /// <seealso cref="HttpHeadTunnelTester"/>
    /// <remarks>
    /// <para>Current implementation supports HTTP (only with CONNECT HTTP method), SOCKS4/4A and SOCKS5 proxy protocols.</para>
    /// <para>Current implementation supports target server to be only HTTP(S) server.</para>
    /// </remarks>
    public static ProxyDiscovery WithTunnelHeadCheck(this ProxyDiscovery discovery, string testUrl)
    {
      if (string.IsNullOrWhiteSpace(testUrl))
        throw new ArgumentNullException(nameof(testUrl));

      discovery.Checker = CreateDefaultProxyChecker(testUrl, new HttpHeadTunnelTester());
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

    /// <summary>
    /// Creates the default proxy checker with default supported proxy protocols: http, socks4, socks5.
    /// </summary>
    /// <param name="testUrl"></param>
    /// <param name="tunnelTester"></param>
    /// <returns></returns>
    public static ProxyChecker CreateDefaultProxyChecker(string testUrl, IProxyTunnelTester tunnelTester)
    {
      if (string.IsNullOrWhiteSpace(testUrl))
        throw new ArgumentNullException(nameof(testUrl));
      if (tunnelTester == null)
        throw new ArgumentNullException(nameof(tunnelTester));

      ProxyChecker checker = new ProxyChecker
      {
        TargetUrl = new Uri(testUrl),
      };

      checker.AddProtocolChecker("http", new HttpConnectChecker());
      checker.AddProtocolChecker("socks4", new Socks4Checker());
      checker.AddProtocolChecker("socks5", new Socks5Checker());

      return checker;
    }
  }
}
