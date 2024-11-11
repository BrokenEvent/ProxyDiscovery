using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Checkers
{
  /// <summary>
  /// Wraps the tunnel tested in SSL stream to support HTTPS. Inner testers will perform the actual testing.
  /// </summary>
  public class SslTunnelTester: IProxyTunnelTester
  {
    public SslTunnelTester(IProxyTunnelTester innerTester)
    {
      InnerTester = innerTester;
    }

    public IProxyTunnelTester InnerTester { get; }

    public async Task<TestResult> TestTunnel(Uri uri, Stream stream, CancellationToken ct)
    {
      // create wrapper stream
      SslStream sslStream = null;
      try
      {
        sslStream = new SslStream(
            stream,
            true,
            (sender, certificate, chain, errors) =>
            {
              // allow to continue only when no errors
              return errors == SslPolicyErrors.None;
            }
          );

        // authenticate
        await sslStream.AuthenticateAsClientAsync(uri.Host);

        // now it's time to check whether there is a cancel request
        if (ct.IsCancellationRequested)
          return new TestResult(ProxyCheckResult.Canceled, "Tunnel check has been canceled");

        // process inner check
        return await InnerTester.TestTunnel(uri, sslStream, ct);
      }
      catch (AuthenticationException e)
      {
        return new TestResult(ProxyCheckResult.SSLError, e.Message);
      }
      finally
      {
        sslStream?.Dispose();
      }
    }

    public TunnelTesterProtocol Protocol
    {
      get { return TunnelTesterProtocol.SSL; }
    }
  }
}
