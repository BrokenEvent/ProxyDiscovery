using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace BrokenEvent.ProxyDiscovery
{
  /// <summary>
  /// Resolver for contry by two-letter code.
  /// </summary>
  public static class CountryResolver
  {
    private static readonly Dictionary<string, string> countries = new Dictionary<string, string>();

    static CountryResolver()
    {
      using (Stream stream = Assembly.GetCallingAssembly().GetManifestResourceStream("BrokenEvent.ProxyDiscovery.countries.csv"))
      {
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
          while (!reader.EndOfStream)
            try
            {
              ParseLine(reader.ReadLine());
            }
            catch
            {
              // ignore
            }
        }
      }
    }

    private static void ParseLine(string line)
    {
      int index = line.IndexOf(';');
      string code = line.Substring(0, index);
      string name = line.Substring(index + 1);

      countries.Add(code, name);
    }

    /// <summary>
    /// Attempts to resolve country by its code.
    /// </summary>
    /// <param name="countryCode">Country code.</param>
    /// <returns>Country name by code.</returns>
    /// <remarks>
    /// <para>If <paramref name="countryCode"/> is <c>null</c> or an empty string - <c>null</c> will be returned.</para>
    /// <para>If <paramref name="countryCode"/> is longer than 2 characters, it will be returned unchanged.</para>
    /// <para>Otherwise the resolver will try to resolve country. If the country is unresolved, the method will return <paramref name="countryCode"/> unchanged.</para>
    /// </remarks>
    public static string Resolve(string countryCode)
    {
      // empty/null
      if (string.IsNullOrWhiteSpace(countryCode))
        return null;

      // not the code, but the name
      if (countryCode.Length > 2)
        return countryCode;

      // resolve
      return countries.TryGetValue(countryCode, out string countryName) ? countryName : countryCode;
    }
  }
}
