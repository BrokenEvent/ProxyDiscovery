using System.Collections.Generic;

using BrokenEvent.ProxyDiscovery.Helpers;
using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Filters
{
  /// <summary>
  /// Base class for location filters.
  /// </summary>
  public abstract class BaseCountryFilter: IProxyFilter
  {
    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      if (CountriesItems.Count == 0)
        yield return "Countries list can't be empty.";
    }

    /// <summary>
    /// Gets the list of countries.
    /// </summary>
    /// <remarks>Updates on set of <see cref="Countries"/></remarks>
    public List<string> CountriesItems { get; } = new List<string>();

    /// <summary>
    /// Gets or sets list of countries in human readable form.
    /// </summary>
    /// <remarks>Updates <see cref="CountriesItems"/> on set.</remarks>
    public string Countries
    {
      get { return string.Join(", ", CountriesItems); }
      set { ParseCountriesList(value, CountriesItems); }
    }

    private static void ParseCountriesList(string value, List<string> list)
    {
      list.Clear();
      if (string.IsNullOrWhiteSpace(value))
        return;

      int i = 0;
      while (true)
      {
        if (!StringHelpers.SkipDelimiters(value, ref i))
          break;

        string s = StringHelpers.GetToken(value, ref i);
        if (s == null)
          break;

        list.Add(s);
      }
    }

    /// <inheritdoc />
    public abstract bool DoesPassFilter(ProxyInformation proxy);
  }
}
