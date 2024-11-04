using System.Collections.Generic;

namespace BrokenEvent.ProxyDiscovery.Helpers
{
  public static class PortSetParser
  {
    public static IEnumerable<PortSet> Parse(string value)
    {
      int i = 0;

      if (string.IsNullOrWhiteSpace(value))
        yield break;

      while (true)
      {
        if (!StringHelpers.SkipSpaces(value, ref i))
          break;

        bool isInverted = false;

        // check for inversion
        if (i < value.Length && value[i] == '~')
        {
          // mark it
          isInverted = true;
          // and skip the inversion symbol
          i++;
        }

        int? value1 = StringHelpers.GetDigits(value, ref i);
        if (!value1.HasValue)
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

            int? value2 = StringHelpers.GetDigits(value, ref i);
            if (!value2.HasValue)
              break;

            if (StringHelpers.SkipSpaces(value, ref i))
            {
              c = value[i];
              if (c == ',' || c == ';')
                i++; // skip comma
            }

            yield return new PortRange((ushort)value1, (ushort)value2, isInverted);
            continue;
          }
        }

        yield return new PortValue((ushort)value1, isInverted);
      }
    }
  }

  public abstract class PortSet
  {
    protected PortSet(bool isInverted)
    {
      IsInverted = isInverted;
    }

    public bool IsInverted { get; }

    public abstract bool Check(ushort port);
  }

  public class PortRange : PortSet
  {
    public ushort MinInclusive { get; }

    public ushort MaxInclusive { get; }

    public PortRange(ushort minInclusive, ushort maxInclusive, bool isInverted)
      : base(isInverted)
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
      return IsInverted ? $"~{MinInclusive}-{MaxInclusive}" : $"{MinInclusive}-{MaxInclusive}";
    }
  }

  public class PortValue : PortSet
  {
    public ushort Value { get; }

    public PortValue(ushort value, bool isInverted)
      : base(isInverted)
    {
      Value = value;
    }

    public override bool Check(ushort port)
    {
      return port == Value;
    }

    public override string ToString()
    {
      return IsInverted ? $"~{Value}" : Value.ToString();
    }
  }
}
