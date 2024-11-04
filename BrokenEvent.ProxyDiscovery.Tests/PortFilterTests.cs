using System;

using BrokenEvent.ProxyDiscovery.Filters;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class PortFilterTests
  {
    public class P
    {
      public readonly string Actual;
      public readonly string Expected;

      public P(string actual, string expected)
      {
        Actual = actual;
        Expected = expected;
      }

      public override string ToString()
      {
        return $"{Actual} → {Expected}";
      }
    }

    public static readonly P[] parsingPositiveValues = new P[]
    {
      new P(null, ""), 

      new P("80", "80"), 
      new P("~80", "~80"), 
      new P("8080", "8080"), 

      new P("80,8080", "80, 8080"), 
      new P("80, 8080", "80, 8080"), 
      new P("80, ~8080", "80, ~8080"), 
      new P("~80, 8080", "~80, 8080"), 
      new P("80 , 8080", "80, 8080"), 
      new P("80 8080", "80, 8080"), 
      new P("80  8080", "80, 8080"), 
      new P("80, 8080,", "80, 8080"), 
      new P("80, 8080, ", "80, 8080"), 
      new P("80 8080 ", "80, 8080"), 

      new P("80,8080,8081", "80, 8080, 8081"), 
      new P("80, 8080, 8081", "80, 8080, 8081"), 
      new P("80 8080 8081", "80, 8080, 8081"), 
      new P("80 8080, 8081", "80, 8080, 8081"), 

      new P("80-81", "80-81"), 
      new P("80 -81", "80-81"), 
      new P("80 - 81", "80-81"), 
      new P("80-8080", "80-8080"), 

      new P("80-100, 120-140", "80-100, 120-140"), 
      new P("80-100, ~120-140", "80-100, ~120-140"), 
      new P("~80-100, 120-140", "~80-100, 120-140"), 
      new P("80-100, 120-140,", "80-100, 120-140"), 
      new P("80-100  120-140", "80-100, 120-140"), 
      new P("80 - 100  120 - 140", "80-100, 120-140"), 
      new P("80 - 100  120 - 140", "80-100, 120-140"),
    };

    [TestCaseSource(nameof(parsingPositiveValues))]
    public void ParsingPositive(P p)
    {
      PortFilter filter = new PortFilter();
      filter.FilterString = p.Actual;

      Assert.AreEqual(p.Expected, filter.FilterString);
    }

    public static readonly P[] parsingNegativeValues = new P[]
    {
      new P("qwerty", null), 
      new P("q80", null), 
      new P("80q", null), 
      new P("80,q81", null), 
      new P("80q,81", null), 
      new P("80q-81", null), 
      new P("80-q81", null), 
      new P("80-81q", null), 
    };

    [TestCaseSource(nameof(parsingNegativeValues))]
    public void ParsingNegative(P p)
    {
      PortFilter filter = new PortFilter();
      Assert.Throws<FormatException>(() => filter.FilterString = p.Actual);
    }

    public class U
    {
      public readonly string Filter;
      public readonly ushort Port;
      public readonly bool Expected;

      public U(string filter, ushort port, bool expected)
      {
        Filter = filter;
        Port = port;
        Expected = expected;
      }

      public override string ToString()
      {
        return $"Filter: '{Filter}', Port: {Port}, Expected: {(Expected ? "Pass" : "Fail")}";
      }
    }

    public static readonly U[] filterValues = new U[]
    {
      new U("80,81", 79, false),
      new U("80,81", 80, true),
      new U("80,81", 81, true),
      new U("80,81", 82, false),

      new U("~80,81", 79, false),
      new U("~80,81", 80, false),
      new U("~80,81", 81, true),
      new U("~80,81", 82, false),

      new U("~80,~81", 79, true),
      new U("~80,~81", 80, false),
      new U("~80,~81", 81, false),
      new U("~80,~81", 82, true),

      new U("80-81", 79, false),
      new U("80-81", 80, true),
      new U("80-81", 81, true),
      new U("80-81", 82, false),

      new U("~80-81", 79, true),
      new U("~80-81", 80, false),
      new U("~80-81", 81, false),
      new U("~80-81", 82, true),

      new U("80,82-85", 80, true),
      new U("80,82-85", 81, false),
      new U("80,82-85", 82, true),
      new U("80,82-85", 85, true),
      new U("80,82-85", 86, false),

      new U("~80,82-85", 80, false),
      new U("~80,82-85", 81, false),
      new U("~80,82-85", 82, true),
      new U("~80,82-85", 85, true),
      new U("~80,82-85", 86, false),

      new U("100-200, ~125", 99, false), 
      new U("100-200, ~125", 100, true), 
      new U("100-200, ~125", 124, true), 
      new U("100-200, ~125", 125, false), 
      new U("100-200, ~125", 126, true), 
      new U("100-200, ~125", 200, true), 
      new U("100-200, ~125", 201, false), 

      new U("100-200, ~120-130", 99, false), 
      new U("100-200, ~120-130", 100, true), 
      new U("100-200, ~120-130", 119, true), 
      new U("100-200, ~120-130", 120, false), 
      new U("100-200, ~120-130", 123, false), 
      new U("100-200, ~120-130", 130, false), 
      new U("100-200, ~120-130", 131, true), 
      new U("100-200, ~120-130", 200, true), 
      new U("100-200, ~120-130", 201, false), 
    };

    [TestCaseSource(nameof(filterValues))]
    public void Filter(U u)
    {
      ProxyInformation proxy = new ProxyInformation("192.168.0.1", u.Port, true);

      PortFilter filter = new PortFilter();
      filter.FilterString = u.Filter;

      Assert.False(filter.Validate().GetEnumerator().MoveNext());

      Assert.AreEqual(u.Expected, filter.DoesPassFilter(proxy));
    }
  }
}
