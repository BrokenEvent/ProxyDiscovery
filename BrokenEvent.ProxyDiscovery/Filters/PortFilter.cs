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
      if (Items.Count == 0)
        yield return "Port filter ports list is missing";
    }

    /// <inheritdoc />
    public bool DoesPassFilter(ProxyInformation proxy)
    {
      foreach (PortFilterItem item in Items)
        if (item.Check(proxy.Port))
          return true;

      return false;
    }

    /// <summary>
    /// Gets the list of port filter items.
    /// </summary>
    /// <remarks>Updates on change of <see cref="FilterString"/>.</remarks>
    public List<PortFilterItem> Items { get; } = new List<PortFilterItem>();

    /// <summary>
    /// Gets or sets the ports list in human-readable form.
    /// </summary>
    /// <remarks>Updates <see cref="Items"/> on set.</remarks>
    public string FilterString
    {
      get { return string.Join(", ", Items); }
      set { ParseFilterString(value); }
    }

    private void ParseFilterString(string value)
    {
      int i = 0;
      Items.Clear();

      if (string.IsNullOrWhiteSpace(value))
        return;

      while (true)
      {
        if (!StringHelpers.SkipSpaces(value, ref i))
          break;

        string s = StringHelpers.GetDigits(value, ref i);
        if (s == null)
          break;

        if (StringHelpers.SkipSpaces(value, ref i))
        {
          char c = value[i];

          if (c == ',' || c == ';')
            i++; // skip that comma
          else if (c == '-')
          {
            i++; // skip dash

            if (!StringHelpers.SkipSpaces(value, ref i))
              break;

            string s1 = StringHelpers.GetDigits(value, ref i);
            if (s1 == null)
              break;

            if (StringHelpers.SkipSpaces(value, ref i))
            {
              c = value[i];
              if (c == ',' || c == ';')
                i++; // skip comma
            }

            Items.Add(new PortFilterRange(ushort.Parse(s), ushort.Parse(s1)));
            continue;
          }
        }

        Items.Add(new PortFilterValue(ushort.Parse(s)));
      }
    }

    public abstract class PortFilterItem
    {
      public abstract bool Check(ushort port);
    }

    public class PortFilterRange: PortFilterItem
    {
      public ushort MinInclusive { get; }

      public ushort MaxInclusive { get; }

      public PortFilterRange(ushort minInclusive, ushort maxInclusive)
      {
        MinInclusive = minInclusive;
        MaxInclusive = maxInclusive;
      }

      public override bool Check(ushort port)
      {
        return port >= MinInclusive && port <= MaxInclusive;
      }

      public override string ToString()
      {
        return $"{MinInclusive}-{MaxInclusive}";
      }
    }

    public class PortFilterValue: PortFilterItem
    {
      public ushort Value { get; }

      public PortFilterValue(ushort value)
      {
        Value = value;
      }

      public override bool Check(ushort port)
      {
        return port == Value;
      }

      public override string ToString()
      {
        return Value.ToString();
      }
    }
  }
}
