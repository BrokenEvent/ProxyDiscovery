using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Checkers
{
  /// <summary>
  /// Checks proxy availability by connecting with the proxy to given URL. Supports only HTTPS proxies (requires proxy to support CONNECT
  /// HTTP method).
  /// </summary>
  public sealed class ProxyChecker: IProxyChecker
  {
    private readonly Dictionary<string, IProxyProtocolChecker> protocols = new Dictionary<string, IProxyProtocolChecker>();

    private IProxyTunnelTester actualTunnelTester;

    /// <summary>
    /// Gets or sets the target URL to check.
    /// </summary>
    /// <remarks>There will be many requests to that server, so please use server which you don't mind.</remarks>
    public Uri TargetUrl { get; set; }

    /// <summary>
    /// Gets or sets the send/receive timeout in milliseconds. The default is 0.
    /// </summary>
    public int Timeout { get; set; }

    /// <summary>
    /// Gets or sets the value indicating whether to cancel gracefully.
    /// </summary>
    /// <remarks>
    /// <para>Non-graceful operation cancel will stop all connections immediately (<see cref="TcpClient.Close"/>) which may lead to different internal
    /// exceptions up to NPE and a possible leak of socket resources.</para>
    /// <para>Graceful operation cancel will wait until the current async operation (socket connect/send/receive) is finished.
    /// In worst cases this may take up to several tens of seconds to completely cancel the proxy discovery.</para>
    /// </remarks>
    public bool GracefulCancel { get; set; } = true;

    /// <summary>
    /// Adds a checker for a specific protocol.
    /// </summary>
    /// <param name="protocol">Lowercase name of the protocol (<see cref="ProxyInformation.Protocol"/>): http, socks4, etc.</param>
    /// <param name="checker">An instance of checker for given protocol.</param>
    /// <seealso cref="GetProtocolChecker"/>
    public void AddProtocolChecker(string protocol, IProxyProtocolChecker checker)
    {
      if (protocol == null)
        throw new ArgumentNullException(nameof(protocol));
      if (checker == null)
        throw new ArgumentNullException(nameof(checker));

      protocols[protocol.ToLower()] = checker;
    }

    /// <summary>
    /// Gets the protocol checker for given protocol by its name.
    /// </summary>
    /// <param name="protocol">Lowercase name of the protocol (<see cref="ProxyInformation.Protocol"/>): http, socks4, etc.</param>
    /// <returns>The instance of checker for given protocol or <c>null</c> if no checker for such protocol registered.</returns>
    /// <seealso cref="AddProtocolChecker"/>
    public IProxyProtocolChecker GetProtocolChecker(string protocol)
    {
      if (string.IsNullOrWhiteSpace(protocol))
        return null;

      return protocols.TryGetValue(protocol, out IProxyProtocolChecker checker) ? checker : null;
    }

    /// <summary>
    /// Gets or sets the proxy tunnel tester.
    /// </summary>
    /// <remarks>
    /// <para>Used to test the tunnel created by a proxy server to the <see cref="TargetUrl"/>.</para>
    /// <para>If the <see cref="TargetUrl"/> uses "https" scheme and tester's protocol is <see cref="TunnelTesterProtocol.Http"/>
    /// it will automatically be wrapped with <see cref="SslTunnelTester"/>.</para>
    /// </remarks>
    public IProxyTunnelTester TunnelTester { get; set; } = new HttpHeadTunnelTester();

    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      if (TargetUrl == null)
        yield return "Target URL cannot be empty.";

      if (TunnelTester == null)
        yield return "Tunnel tester must not be null.";
      else if (TargetUrl.Scheme == "http" && TunnelTester.Protocol == TunnelTesterProtocol.SSL)
        yield return $"The tunnel tester {TunnelTester.GetType().Name} uses SSL, but the target URL uses http scheme.";

      if (protocols.Count == 0)
        yield return "No protocol checkers provided";
      else
        foreach (IProxyProtocolChecker checker in protocols.Values)
          foreach (string s in checker.Validate())
            yield return s;
    }

    /// <inheritdoc />
    public void Prepare()
    {
      // run prepare for per-protocol checkers
      foreach (IProxyProtocolChecker checker in protocols.Values)
        checker.Prepare(TargetUrl);

      // process the tunnel tester
      switch (TunnelTester.Protocol)
      {
        case TunnelTesterProtocol.Http:
          if (TargetUrl.Scheme == "https")
            // wrap with SSL tester
            actualTunnelTester = new SslTunnelTester(TunnelTester);
          else
            // otherwise leave it as it is
            goto default;
          break;

        case TunnelTesterProtocol.SSL:
          // the compatibility is checked in Validate()
          goto default;

        default:
          actualTunnelTester = TunnelTester;
          break;
      }
    }

    /// <inheritdoc />
    public async Task<ProxyState> CheckProxy(ProxyInformation proxy, CancellationToken ct)
    {
      // respect the cancellation token
      if (ct.IsCancellationRequested)
        return new ProxyState(proxy, ProxyCheckResult.Canceled, "Check has been canceled", TimeSpan.Zero);

      if (string.IsNullOrWhiteSpace(proxy.Protocol))
        return new ProxyState(proxy, ProxyCheckResult.Unchecked, "Cannot check proxy with unknown protocol", TimeSpan.Zero);

      // get the checker for proxy's protocol
      IProxyProtocolChecker protocolChecker = GetProtocolChecker(proxy.Protocol);

      // check it is not null
      if (protocolChecker == null)
        return new ProxyState(proxy, ProxyCheckResult.Unchecked, $"Unable to get protocol checker for {proxy.Protocol}", TimeSpan.Zero);

      TcpClient client = new TcpClient();
      client.LingerState.Enabled = false;
      client.NoDelay = true;
      client.SendTimeout = Timeout;
      client.ReceiveTimeout = Timeout;

      Stopwatch sw = Stopwatch.StartNew();

      // if we intent to cancel non-gracefully, register TcpClient.Close() to be called once the cancel is received. Only if.
      CancellationTokenRegistration? ctr = null;
      if (!GracefulCancel)
        ctr = ct.Register(client.Close);

      try
      {
        // attempt to connect to target host
        await client.ConnectAsync(proxy.Address, proxy.Port).ConfigureAwait(false);

        // respect the cancellation token
        if (ct.IsCancellationRequested)
          return new ProxyState(proxy, ProxyCheckResult.Canceled, "Check has been canceled", TimeSpan.Zero);

        NetworkStream stream = client.GetStream();

        // test the connection depending on protocol
        TestResult protocolTestResult = await protocolChecker.TestConnection(TargetUrl, proxy, stream, ct);

        // in case of protocol test failed
        if (protocolTestResult.Result != ProxyCheckResult.OK)
          return new ProxyState(proxy, protocolTestResult.Result, protocolTestResult.Message, sw.Elapsed);

        // respect the cancellation token
        if (ct.IsCancellationRequested)
          return new ProxyState(proxy, ProxyCheckResult.Canceled, "Check has been canceled", TimeSpan.Zero);

        // test the tunnel created by protocol
        TestResult tunnelTestResult = await actualTunnelTester.TestTunnel(TargetUrl, stream, ct);

        // and can return the test result
        return new ProxyState(proxy, tunnelTestResult.Result, tunnelTestResult.Message, sw.Elapsed);
      }
      catch (SocketException e)
      {
        return new ProxyState(proxy, ProxyCheckResult.NetworkError, e.Message, sw.Elapsed);
      }
      catch (IOException e)
      {
        return new ProxyState(proxy, ProxyCheckResult.NetworkError, e.Message, sw.Elapsed);
      }
      catch (OperationCanceledException)
      {
        return new ProxyState(proxy, ProxyCheckResult.Canceled, "Check has been canceled", TimeSpan.Zero);
      }
      catch (Exception e)
      {
        // we can encounter different types of exceptions on cancel, even NPEs, so don't log them.
        if (ct.IsCancellationRequested)
          return new ProxyState(proxy, ProxyCheckResult.Canceled, "Check has been canceled", TimeSpan.Zero);

        return new ProxyState(proxy, ProxyCheckResult.Failure, e.Message, TimeSpan.Zero);
      }
      finally
      {
        ctr?.Dispose();
        client.Dispose();
      }
    }
  }
}
