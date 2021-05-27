using System.Collections.Generic;

using BrokenEvent.ProxyDiscovery.Helpers;
using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Filters
{
  /// <summary>
  /// Base class for location filters.
  /// </summary>
  public abstract class LocationBaseFilter: IProxyFilter
  {
    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      if (LocationItems.Count == 0)
        yield return "Countries list can't be empty.";
    }

    /// <summary>
    /// Gets the list of locations.
    /// </summary>
    /// <remarks>Updates on set of <see cref="Locations"/></remarks>
    public List<string> LocationItems { get; } = new List<string>();

    /// <summary>
    /// Gets or sets list of locations in human readable form.
    /// </summary>
    /// <remarks>Updates <see cref="LocationItems"/> on set.</remarks>
    public string Locations
    {
      get { return string.Join(", ", LocationItems); }
      set { ParseCountriesList(value, LocationItems); }
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
