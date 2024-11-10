﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using BrokenEvent.ProxyDiscovery.Checkers;

namespace BrokenEvent.ProxyDiscovery.Interfaces
{
  /// <summary>
  /// Performs check of the TCP tunnel created by the proxy server.
  /// </summary>
  public interface IProxyTunnelTester
  {
    /// <summary>
    /// Checks the TCP tunnel created by proxy.
    /// </summary>
    /// <param name="uri">Target URL set by user to check.</param>
    /// <param name="stream">TCP tunnel stream.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Proxy check result.</returns>
    /// <remarks>The method can freely throw exceptions as they will be handled in <see cref="ProxyHttpConnectChecker.CheckProxy"/>.</remarks>
    Task<TunnelTestResult> CheckTunnel(Uri uri, Stream stream, CancellationToken ct);

    /// <summary>
    /// Gets the protocol rules for the tunnel tester.
    /// </summary>
    TunnelTesterProtocol Protocol { get; }
  }

  public struct TunnelTestResult
  {
    public ProxyCheckResult Result;
    public string Message;

    public TunnelTestResult(ProxyCheckResult result, string message)
    {
      Result = result;
      Message = message;
    }
  }

  /// <summary>
  /// Protocol rules for the tunnel tester.
  /// </summary>
  public enum TunnelTesterProtocol
  {
    /// <summary>
    /// The tunnel tester works with HTTP protocol.
    /// </summary>
    /// <remarks>If the target URL uses "https" scheme the tester will be wrapped with SSL-supporting tester.</remarks>
    Http,
    /// <summary>
    /// The tunnel tester works with SSL.
    /// </summary>
    /// <remarks>If the target URL uses "http" (not "https") scheme, an exception will be thrown.</remarks>
    SSL,
    /// <summary>
    /// The tester cares about the protocol by itself.
    /// </summary>
    DontCare
  }
}