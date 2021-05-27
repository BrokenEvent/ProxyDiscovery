using System;
using System.Net;
using System.Text;

namespace BrokenEvent.ProxyDiscovery
{
  public sealed class ProxyInformation: IEquatable<ProxyInformation>
  {
    public ProxyInformation(IPAddress address, ushort port, bool? isHttps = null, string location = null, bool? googlePassed = null)
    {
      if (address == null)
        throw new ArgumentNullException(nameof(address));

      Address = address;
      Port = port;
      IsHttps = isHttps;
      GooglePassed = googlePassed;
      Location = location;
    }

    public ProxyInformation(string address, ushort port, bool? isHttps = null, string location = null, bool? googlePassed = null):
      this(IPAddress.Parse(address), port, isHttps, location, googlePassed)
    {
    }

    /// <summary>
    /// Gets the IP address of the proxy.
    /// </summary>
    public IPAddress Address { get; }

    /// <summary>
    /// Gets the port of the proxy.
    /// </summary>
    public ushort Port { get; }

    /// <summary>
    /// Gets the value indicating whether the proxy supports HTTPS.
    /// </summary>
    public bool? IsHttps { get; }

    /// <summary>
    /// Gets the geographical location of the proxy.
    /// </summary>
    public string Location { get; }

    /// <summary>
    /// Whether the proxy can use Google search.
    /// </summary>
    public bool? GooglePassed { get; }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append(Address).Append(":").Append(Port).Append(", ");

      if (IsHttps.HasValue)
        sb.Append(IsHttps.Value ? "HTTPS" : "HTTP").Append(", ");

      if (GooglePassed.HasValue)
        sb.Append("Google: ").Append(GooglePassed.Value ? "yes" : "no").Append(", ");

      if (Location != null)
        sb.Append("Location: ").Append(Location);

      return sb.ToString();
    }

    /// <summary>
    /// Converts the proxy to socket address string.
    /// </summary>
    /// <returns>Proxy value in IP:Port string form</returns>
    public string ToAddress()
    {
      return $"{Address}:{Port}";
    }

    /// <summary>
    /// Converts the proxy to <see cref="WebProxy"/> for <see cref="WebRequest"/>.
    /// </summary>
    /// <returns>Web proxy.</returns>
    public WebProxy ToWebProxy()
    {
      return new WebProxy(ToAddress());
    }

    /// <summary>
    /// Converts the proxy to <see cref="IPEndPoint"/>.
    /// </summary>
    /// <returns>IP EndPoint.</returns>
    public IPEndPoint ToEndPoint()
    {
      return new IPEndPoint(Address, Port);
    }

    #region Implicit cast

    public static implicit operator WebProxy (ProxyInformation proxy)
    {
      return proxy.ToWebProxy();
    }

    public static implicit operator IPEndPoint(ProxyInformation proxy)
    {
      return proxy.ToEndPoint();
    }

    #endregion

    #region Equality

    public bool Equals(ProxyInformation other)
    {
      if (ReferenceEquals(null, other))
        return false;
      if (ReferenceEquals(this, other))
        return true;
      return Equals(Address.Equals(other.Address)) && Port == other.Port;
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj))
        return false;
      if (ReferenceEquals(this, obj))
        return true;
      if (obj.GetType() != GetType())
        return false;
      return Equals((ProxyInformation)obj);
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return ((Address != null ? Address.GetHashCode() : 0) * 397) ^ Port.GetHashCode();
      }
    }

    public static bool operator ==(ProxyInformation left, ProxyInformation right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(ProxyInformation left, ProxyInformation right)
    {
      return !Equals(left, right);
    }

    #endregion
  }
}
