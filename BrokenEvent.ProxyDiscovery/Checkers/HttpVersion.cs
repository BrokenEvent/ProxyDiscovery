namespace BrokenEvent.ProxyDiscovery.Checkers
{
  public enum HttpVersion
  {
    /// <summary>
    /// Test on HTTP/1.0
    /// </summary>
    OneZero,
    /// <summary>
    /// Test on HTTP/1.1
    /// </summary>
    /// <remarks>The only difference is that Host header is required for 1.1.</remarks>
    OneOne
  }
}