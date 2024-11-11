using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BrokenEvent.ProxyDiscovery.Helpers
{
  /// <summary>
  /// https://datatracker.ietf.org/doc/html/rfc1928
  /// </summary>
  public struct Socks5Parser
  {
    #region Static

    public static byte[] BuildMethodRequest(params Socks5Method[] methods)
    {
      if (methods == null || methods.Length == 0)
        throw new ArgumentNullException(nameof(methods));

      byte[] bytes = new byte[2 + methods.Length];

      int index = 0;

      // version
      bytes[index++] = 5;
      // methods number
      bytes[index++] = (byte)methods.Length;

      // the methods 
      foreach (Socks5Method method in methods)
        bytes[index++] = (byte)method;

      return bytes;
    }

    public static Socks5Method ParseMethodResponse(byte[] bytes)
    {
      // ignore the first byte as it is not described in RFC 1928
      return (Socks5Method)bytes[1];
    }

    public static byte[] BuildRequest(Socks5Command command, IPAddress targetAddress, ushort targetPort)
    {
      if (targetAddress == null)
        throw new ArgumentNullException(nameof(targetAddress));

      switch (targetAddress.AddressFamily)
      {
        case AddressFamily.InterNetwork:
          return BuildRequest(command, targetAddress.GetAddressBytes(), Socks5AddressType.IpV4, targetPort);

        case AddressFamily.InterNetworkV6:
          return BuildRequest(command, targetAddress.GetAddressBytes(), Socks5AddressType.IpV6, targetPort);

        default:
          throw new NotSupportedException($"Address family not supported: {targetAddress.AddressFamily}");
      }
    }

    public static byte[] BuildRequest(Socks5Command command, string targetAddress, ushort targetPort)
    {
      if (string.IsNullOrWhiteSpace(targetAddress))
        throw new ArgumentNullException(nameof(targetAddress));

      byte[] address = new byte[targetAddress.Length + 1];
      address[0] = (byte)targetAddress.Length;
      Encoding.ASCII.GetBytes(targetAddress, 0, targetAddress.Length, address, 1);

      return BuildRequest(command, address, Socks5AddressType.DomainName, targetPort);
    }

    private static byte[] BuildRequest(Socks5Command command, byte[] address, Socks5AddressType addressType, ushort targetPort)
    {
      // projected size
      // 1 + 1 + 1 + 1 + address length + 2
      byte[] bytes = new byte[6 + address.Length];

      int index = 0;
      // version
      bytes[index++] = 5;

      // command
      bytes[index++] = (byte)command;

      // skip reserved byte
      index++;

      // address type
      bytes[index++] = (byte)addressType;

      // copy address
      Array.Copy(address, 0, bytes, index, address.Length);
      index += address.Length;

      // target port, network byte order
      bytes[index++] = (byte)((targetPort >> 8) & 0xFF);
      bytes[index++] = (byte)(targetPort & 0xFF);

      return bytes;
    }

    #endregion

    public readonly Socks5Reply Reply;
    public readonly IPAddress BindAddress;
    public readonly string BindDomainName;
    public readonly ushort BindPort;

    public Socks5Parser(byte[] bytes)
      : this()
    {
      int index = 0;

      // version
      if (bytes[index++] != 5)
      {
        Reply = Socks5Reply.InvalidPacket;
        return;
      }

      // reply code
      Reply = (Socks5Reply)bytes[index++];

      // ok?
      if (Reply != Socks5Reply.OK)
        return;

      // skip reserved byte
      index++;

      // address type
      Socks5AddressType addressType = (Socks5AddressType)bytes[index++];

      byte[] address;

      switch (addressType)
      {
        case Socks5AddressType.IpV4:
          address = new byte[4];
          Array.Copy(bytes, index, address, 0, 4);
          BindAddress = new IPAddress(address);
          index += 4;
          break;

        case Socks5AddressType.IpV6:
          address = new byte[4];
          Array.Copy(bytes, index, address, 0, 16);
          BindAddress = new IPAddress(address);
          index += 16;
          break;

        case Socks5AddressType.DomainName:
          // length
          address = new byte[index++];
          // data
          Array.Copy(bytes, index, address, 0, address.Length);
          index += address.Length;
          // decode
          BindDomainName = Encoding.ASCII.GetString(address);
          break;

        default:
          Reply = Socks5Reply.InvalidPacket;
          return;
      }

      // get port, network bytes order
      BindPort = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(bytes, index));
    }
  }

  public enum Socks5Method: byte
  {
    /// <summary>
    /// No authentication required.
    /// </summary>
    None = 0,
    /// <summary>
    /// GSSAPI.
    /// </summary>
    GSSAPI = 1,
    /// <summary>
    /// Username and password (RFC 1929)
    /// </summary>
    UsernameAndPassword = 2,
    /// <summary>
    /// CHAP method.
    /// </summary>
    CHAP = 3,
    /// <summary>
    /// Challenge-response method.
    /// </summary>
    ChallengeResponse=5,
    /// <summary>
    /// SSL (?)
    /// </summary>
    SSL = 6,
    /// <summary>
    /// LDAP
    /// </summary>
    LDAP = 7,
    /// <summary>
    /// Multifactor authentication framework (?)
    /// </summary>
    Multifactor = 8,
    /// <summary>
    /// JSON (?)
    /// </summary>
    JSON = 9,
    /// <summary>
    /// No acceptable methods, server response.
    /// </summary>
    NoAcceptableMethods = 255,
  }

  public enum Socks5Command: byte
  {
    Connect = 1,
    Bind = 2,
    UdpAssociate = 3
  }

  public enum Socks5AddressType: byte
  {
    IpV4 = 1,
    DomainName = 3,
    IpV6 = 4
  }

  public enum Socks5Reply: byte
  {
    /// <summary>
    /// Success
    /// </summary>
    OK = 0,
    /// <summary>
    /// General Socks server failure.
    /// </summary>
    ServerFailure = 1,
    /// <summary>
    /// Connection not allowed by ruleset.
    /// </summary>
    NotAllowed = 2,
    /// <summary>
    /// Network Unreachable.
    /// </summary>
    NetworkUnreachable = 3,
    /// <summary>
    /// Host unreachable.
    /// </summary>
    HostUnreachable = 4,
    /// <summary>
    /// Connection refused.
    /// </summary>
    ConnectionRefused = 5,
    /// <summary>
    /// TTL Expired
    /// </summary>
    TTLExpired = 6,
    /// <summary>
    /// Command not supported
    /// </summary>
    CommandNotSupported = 7,
    /// <summary>
    /// Address type not supported.
    /// </summary>
    AddressTypeNotSupported = 8,

    /// <summary>
    /// The packet is invalid (own error code)
    /// </summary>
    InvalidPacket = 100,
  }
}
