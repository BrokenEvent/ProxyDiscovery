using System;
using System.Net;
using System.Text;

using BrokenEvent.ProxyDiscovery.Helpers;

namespace BrokenEvent.ProxyDiscovery
{
  public sealed class ProxyInformation: IEquatable<ProxyInformation>
  {
    public ProxyInformation(
        IPAddress address,
        ushort port,
        string protocol,
        bool? isSsl = null,
        bool? isGooglePassed = null,
        string name = null,
        string country = null,
        string city = null
      )
    {
      if (address == null)
        throw new ArgumentNullException(nameof(address));
      if (string.IsNullOrWhiteSpace(protocol))
        throw new ArgumentNullException(nameof(protocol));

      Address = address;
      Port = port;
      IsSSL = isSsl;
      Country = CountryResolver.Resolve(country);
      City = city;
      Name = name;
      Protocol = protocol.ToLower();
      IsGooglePassed = isGooglePassed;
      Server = ServiceDetector.DetectService(port);
    }

    public ProxyInformation(
        string address,
        ushort port,
        string protocol,
        bool? isSsl = null,
        bool? isGooglePassed = null,
        string name = null,
        string country = null,
        string city = null
      )
      : this(
          IPAddress.Parse(address),
          port,
          protocol,
          isSsl,
          isGooglePassed,
          name,
          country,
          city
        )
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
    /// Gets or sets the value indicating whether the proxy supports SSL/TCP tunnelling.
    /// </summary>
    public bool? IsSSL { get; set; }

    /// <summary>
    /// Gets the proxy protocol.
    /// </summary>
    /// <remarks>Protocol uses lowercase names like: http, socks4, etc.</remarks>
    public string Protocol { get; }

    /// <summary>
    /// Gets or sets the value indicating whether the proxy can be passed to the Google services.
    /// </summary>
    public bool? IsGooglePassed { get; set; }

    /// <summary>
    /// Gets the name of the proxy, if any.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the country of the proxy, if any.
    /// </summary>
    public string Country { get; }

    /// <summary>
    /// Gets the city of the proxy, if any.
    /// </summary>
    public string City { get; }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append(Address).Append(":").Append(Port);
      sb.AppendItem("Protocol", Protocol);

      if (IsSSL.HasValue)
        sb.AppendItem(IsSSL.Value ? "SSL" : "non-SSL");

      if (IsGooglePassed.HasValue)
        sb.AppendItem("Google", IsGooglePassed.Value ? "yes" : "no");

      sb.AppendItem("Name", Name);
      sb.AppendItem("Country", Country);
      sb.AppendItem("City", City);

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
    /// Gets the guess about proxy server service name by its port.
    /// </summary>
    /// <see cref="ServiceDetector"/>
    public string Server { get; }

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
