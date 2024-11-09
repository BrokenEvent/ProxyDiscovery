using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Checkers
{
  /// <summary>
  /// Stub tunnel tester which does not perform any testing.
  /// </summary>
  public class NoneTunnelTester: IProxyTunnelTester
  {
    public Task<TunnelTestResult> CheckTunnel(Uri uri, Stream stream, CancellationToken ct)
    {
      return Task.FromResult(new TunnelTestResult(ProxyCheckResult.OK, "Tunnel not checked"));
    }

    public TunnelTesterProtocol Protocol
    {
      get { return TunnelTesterProtocol.DontCare; }
    }
  }
}
