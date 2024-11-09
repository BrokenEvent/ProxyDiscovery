using System;
using System.Text;

using BrokenEvent.ProxyDiscovery.Checkers;

namespace BrokenEvent.ProxyDiscovery.Helpers
{
  public static class HttpRequestBuilder
  {
    private const string DELIMITER = "\r\n";

    /// <summary>
    /// Builds an HTTP request.
    /// </summary>
    /// <param name="version">HTTP version.</param>
    /// <param name="httpMethod">HTTP method to use.</param>
    /// <param name="host">Target host. Used for Host header (in case of HTTP version <see cref="HttpVersion.OneOne"/>).</param>
    /// <param name="port">Optional port. <c>null</c> means don't specify port. Used for Host header (in case of HTTP version <see cref="HttpVersion.OneOne"/>).</param>
    /// <param name="resource">Resource path to request. If <c>null</c>, <paramref name="host"/> and optionally <paramref name="port"/> will be used.</param>
    /// <returns>HTTP request text.</returns>
    public static byte[] BuildRequest(HttpVersion version, string httpMethod, string host, int? port, string resource = null)
    {
      if (httpMethod == null)
        throw new ArgumentNullException(nameof(httpMethod));
      if (host == null)
        throw new ArgumentNullException(nameof(host));

      StringBuilder sb = new StringBuilder();
      // method
      sb.Append(httpMethod).Append(" ");

      // resource or host[:port]
      if (resource == null)
      {
        sb.Append(host);
        if (port.HasValue)
          sb.Append(":").Append(port.Value);
      }
      else
        sb.Append(resource);

      // protocol and version
      sb.Append(" HTTP/");
      switch (version)
      {
        case HttpVersion.OneZero:
          sb.Append("1.0");
          break;

        case HttpVersion.OneOne:
          sb.Append("1.1");
          break;

        default:
          throw new InvalidOperationException($"Invalid or unsupported HTTP version: {version}");
      }

      // line delimiter after the first line
      sb.Append(DELIMITER);

      // Host header is required for HTTP/1.1
      if (version == HttpVersion.OneOne)
      {
        sb.Append("Host:").Append(host);
        if (port.HasValue)
          sb.Append(":").Append(port.Value);

        // line delimiter after the Host header
        sb.Append(DELIMITER);
      }

      // one more delimiter to finish headers
      sb.Append(DELIMITER);

      return Encoding.ASCII.GetBytes(sb.ToString());
    }
  }
}
