using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BrokenEvent.ProxyDiscovery.Helpers
{
  /// <summary>
  /// https://github.com/bolv88/socks4a/blob/master/socks4.protocol
  /// </summary>
  public static class Socks4Builder
  {
    /// <summary>
    /// Builds an SOCKS4 connect request.
    /// </summary>
    /// <param name="targetAddress">Target IP address. Only IPv4 addresses are supported.</param>
    /// <param name="targetPort">Target port.</param>
    /// <param name="userId">SOCKS4 user id.</param>
    /// <returns>Binary request packet.</returns>
    public static byte[] BuildConnectRequest(IPAddress targetAddress, ushort targetPort, string userId = "ProxyDiscovery")
    {
      if (targetAddress == null)
        throw new ArgumentNullException(nameof(targetAddress));
      if (targetAddress.AddressFamily != AddressFamily.InterNetwork)
        throw new ArgumentException($"Only IPv4 addresses supported, but {targetAddress} provided", nameof(targetAddress));

      // projected size
      // 1 + 1 + 2 + 4 + variable + 1
      byte[] result = new byte[9 + (userId == null ? 0 : userId.Length)];

      int index = 0;

      // version
      result[index++] = 4;
      // command
      result[index++] = (byte)Socks4Command.Connect;

      // target port, network byte order
      result[index++] = (byte)((targetPort >> 8) & 0xFF);
      result[index++] = (byte)(targetPort & 0xFF);

      // get IPv4 address as bytes
      byte[] address = targetAddress.GetAddressBytes();
      // and copy to target array
      Array.Copy(address, 0, result, 4, 4);
      index += 4;

      if (userId != null)
        Encoding.ASCII.GetBytes(userId, 0, userId.Length, result, index);

      // leave the last byte non-initialized as it is already null

      return result;
    }

    /// <summary>
    /// Parses the Socks4 server response.
    /// </summary>
    /// <param name="bytes">Response binary packet.</param>
    /// <returns>The result of Socks4 request.</returns>
    public static Socks4Result ParseConnectResponse(byte[] bytes)
    {
      if (bytes == null)
        throw new ArgumentNullException(nameof(bytes));

      // should be 0
      if (bytes[0] != 0)
        return Socks4Result.Invalid;

      // 2nd byte is valuable
      // the others are to ignore
      return (Socks4Result)bytes[1];
    }
  }

  public enum Socks4Command: byte
  {
    Connect = 1,
    Bind = 2,
  }

  public enum Socks4Result : byte
  {
    /// <summary>
    /// The response is invalid.
    /// </summary>
    Invalid,
    /// <summary>
    /// Request granted.
    /// </summary>
    OK = 90,
    /// <summary>
    /// Request rejected or failed.
    /// </summary>
    Failed = 91,
    /// <summary>
    /// Request rejected becasue SOCKS server cannot connect to identd on the client
    /// </summary>
    Rejected = 92,
    /// <summary>
    /// Request rejected because the client program and identd report different user-ids
    /// </summary>
    UserIdMismatch = 93
  }
}
