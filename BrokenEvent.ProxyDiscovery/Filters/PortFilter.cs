using System.Collections.Generic;

using BrokenEvent.ProxyDiscovery.Helpers;
using BrokenEvent.ProxyDiscovery.Interfaces;

namespace BrokenEvent.ProxyDiscovery.Filters
{
  /// <summary>
  /// Filters proxies by port(s).
  /// </summary>
  public sealed class PortFilter: IProxyFilter
  {
    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
      if (Filters.Count == 0)
        yield return "Port filter ports lists are empty.";
    }

    /// <inheritdoc />
    public bool DoesPassFilter(ProxyInformation proxy)
    {
      // do we have include filters at all?
      bool hasIncludes = false;
      // does our port included?
      bool isIncluded = false;

      foreach (PortSet item in Filters)
      {
        // not an include filter
        if (item.IsInverted)
          continue;

        // at least one include filter exists
        hasIncludes = true;

        // does not pass
        if (!item.Check(proxy.Port))
          continue;

        // pass - so mark it
        isIncluded = true;
        break;
      }

      // if we have include filters, but the port doesn't pass any of those
      if (hasIncludes && !isIncluded)
        return false;

      // exclude
      foreach (PortSet item in Filters)
      {
        // not an exclude filter
        if (!item.IsInverted)
          continue;

        if (item.Check(proxy.Port))
          return false; // meets the exclude criteria, so drop it
      }

      return true;
    }

    /// <summary>
    /// Gets the list of port filter items.
    /// </summary>
    /// <remarks>Updates on change of <see cref="FilterString"/>.</remarks>
    public List<PortSet> Filters { get; } = new List<PortSet>();

    /// <summary>
    /// Gets or sets the ports list in human-readable form.
    /// </summary>
    /// <remarks>
    /// <para>Updates <see cref="Filters"/> on set.</para>
    /// <para>Syntax uses commas and dashes. "80, 81" means two separate ports (<c>port == 80 || port == 81</c>).
    /// "80-90" means interval, exclusively (<c>port >= 80 &amp;&amp; port &lt;= 90</c>).
    /// Those can be mixed as "80, 90-100, 3128".</para>
    /// <para>When a value is prefixed with ~, it is counted as an exclude filter: "~80" (include all except 80), "~80-100" (include all except within range from
    ///  80 to 100), "80-100, ~91" (include all from 80 to 100, but not 91).</para>
    /// <para>No include filters means to allow everything (except the exclusions).</para>
    /// </remarks>
    public string FilterString
    {
      get { return string.Join(", ", Filters); }
      set { ParseFilterString(value); }
    }

    private void ParseFilterString(string value)
    {
      Filters.Clear();
      Filters.AddRange(PortSetParser.Parse(value));
    }
  }
}
