using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace BrokenEvent.ProxyDiscovery
{
  /// <summary>
  /// Detector of proxy server (app) by its default port.
  /// </summary>
  public static class ServiceDetector
  {
    private static readonly List<ServiceValue> serviceValues = new List<ServiceValue>();

    public const string CustomResult = "<custom>";

    static ServiceDetector()
    {
      using (Stream stream = Assembly.GetCallingAssembly().GetManifestResourceStream("BrokenEvent.ProxyDiscovery.services.csv"))
      {
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
          while (!reader.EndOfStream)
            try
            {
              serviceValues.Add(new ServiceValue(reader.ReadLine()));
            }
            catch
            {
              // ignore
            }
        }
      }
    }

    /// <summary>
    /// Attempts to detect the proxy server app by the port used. Might give meaningful results only if the server uses its default port.
    /// </summary>
    /// <param name="port">Port number to detect for.</param>
    /// <returns>One or more (combined with comma and "or") proxy server names found by this port. If nothing found, <see cref="CustomResult"/> result is used.</returns>
    /// <example>For port 80 the result would be "Nginx Proxy, WinGate, HAProxy or Varnish Cache". These proxy servers use 80th port as the default.</example>
    /// <remarks>Uses predefined table collected by ChatGPT. May be subject of change if more complete data appear.</remarks>
    public static string DetectService(ushort port)
    {
      List<string> results = null;
      string result = null;

      foreach (ServiceValue service in serviceValues)
      {
        if (!service.CheckPort(port))
          continue;

        if (result == null)
        {
          result = service.Name;
          continue;
        }

        if (results == null)
        {
          results = new List<string>();
          results.Add(result);
        }

        results.Add(service.Name);
      }

      if (results == null)
        return result ?? CustomResult;

      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < results.Count - 1; i++)
      {
        if (sb.Length > 0)
          sb.Append(", ");
        sb.Append(results[i]);
      }

      sb.Append(" or ").Append(results[results.Count - 1]);
      return sb.ToString();
    }

    private class ServiceValue
    {
      private readonly ushort[] ports;

      public ServiceValue(string line)
      {
        string[] split = line.Split(',');

        Name = split[0];

        ports = new ushort[split.Length - 1];
        for (int i = 1; i < split.Length; i++)
          ports[i - 1] = ushort.Parse(split[i]);
      }

      public string Name { get; }

      public bool CheckPort(ushort port)
      {
        for (int i = 0; i < ports.Length; i++)
          if (port == ports[i])
            return true;

        return false;
      }
    }
  }
}
