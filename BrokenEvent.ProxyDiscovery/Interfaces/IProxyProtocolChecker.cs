using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BrokenEvent.ProxyDiscovery.Interfaces
{
  public interface IProxyProtocolChecker: IValidatable
  {
    /// <summary>
    /// Performs the preparative/initializative tasks for the protocol.
    /// </summary>
    /// <param name="targetUrl">The URL of the test target.</param>
    void Prepare(Uri targetUrl);

    /// <summary>
    /// Performs the connect request to the proxy server on given connection.
    /// </summary>
    /// <param name="targetUrl">The URL of the test target.</param>
    /// <param name="proxy">Proxy information.</param>
    /// <param name="stream">Network stream to process connect on.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Proxy test result.</returns>
    /// <remarks>The result of <see cref="ProxyCheckResult.OK"/> means the TCP tunnel is created and can be tested. Other results means
    /// the proxy server test is failed.</remarks>
    Task<TestResult> TestConnection(Uri targetUrl, ProxyInformation proxy, NetworkStream stream, CancellationToken ct);
  }
}
