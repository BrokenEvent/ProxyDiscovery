using System;

namespace BrokenEvent.ProxyDiscovery
{
  /// <summary>
  /// State or proxy availability check.
  /// </summary>
  public sealed class ProxyState: IEquatable<ProxyState>
  {
    /// <summary>
    /// Creates an instance of the proxy state.
    /// </summary>
    /// <param name="proxy">Proxy.</param>
    /// <param name="result">Proxy check result.</param>
    /// <param name="status">Proxy check result text status.</param>
    /// <param name="delay">Time to connect.</param>
    public ProxyState(ProxyInformation proxy, ProxyCheckResult result, string status, TimeSpan delay)
    {
      Proxy = proxy;
      Result = result;
      Status = status;
      Delay = delay;
    }

    /// <summary>
    /// Gets the proxy.
    /// </summary>
    public ProxyInformation Proxy { get; }

    /// <summary>
    /// Gets the proxy check result.
    /// </summary>
    public ProxyCheckResult Result { get; }

    /// <summary>
    /// Gets the text status of the proxy check.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets the time it took to connect or to fail.
    /// </summary>
    public TimeSpan Delay { get; }

    public override string ToString()
    {
      return $"{Proxy} - {Result}, {Delay.TotalMilliseconds} ms";
    }

    #region Equality

    public bool Equals(ProxyState other)
    {
      if (ReferenceEquals(null, other))
        return false;
      if (ReferenceEquals(this, other))
        return true;
      return Equals(Proxy, other.Proxy) && string.Equals(Result, other.Result);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj))
        return false;
      if (ReferenceEquals(this, obj))
        return true;
      return obj is ProxyState && Equals((ProxyState)obj);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return ((Proxy != null ? Proxy.GetHashCode() : 0) * 397) ^ (Status != null ? Status.GetHashCode() : 0);
      }
    }

    public static bool operator ==(ProxyState left, ProxyState right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(ProxyState left, ProxyState right)
    {
      return !Equals(left, right);
    }

    #endregion
  }
}
