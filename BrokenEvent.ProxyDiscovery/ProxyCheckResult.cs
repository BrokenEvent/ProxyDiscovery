namespace BrokenEvent.ProxyDiscovery
{
  /// <summary>
  /// Generalized proxy check results.
  /// </summary>
  public enum ProxyCheckResult
  {
    /// <summary>
    /// Proxy availability check was not performed.
    /// </summary>
    Unckeched = -1,
    /// <summary>
    /// Proxy is available and working.
    /// </summary>
    OK,
    /// <summary>
    /// Couldn't connect to proxy.
    /// </summary>
    NetworkError,
    /// <summary>
    /// Proxy server refuses to perform the service.
    /// </summary>
    ServiceRefused,
    /// <summary>
    /// Proxy checker was unable to parse proxy server response.
    /// </summary>
    UnparsableResponse,
    /// <summary>
    /// Unspecified error, see <see cref="ProxyState.Status"/> for details.
    /// </summary>
    Failure,
    /// <summary>
    /// The check has been canceled.
    /// </summary>
    Canceled,
  }
}