using BrokenEvent.ProxyDiscovery.Filters;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class CountryFilterTests
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
      new P("Malaysia", "Malaysia"), 
      new P("Malaysia ", "Malaysia"), 
      new P("Malaysia,", "Malaysia"), 

      new P("Malaysia,Philippines", "Malaysia, Philippines"), 
      new P("Malaysia, Philippines", "Malaysia, Philippines"), 
      new P("Malaysia Philippines", "Malaysia, Philippines"),
      new P("Malaysia Philippines,", "Malaysia, Philippines"),
      new P("Malaysia Philippines ", "Malaysia, Philippines"),

      new P("Malaysia,Philippines,India", "Malaysia, Philippines, India"),
      new P("Malaysia Philippines,India", "Malaysia, Philippines, India"),
      new P("Malaysia Philippines,India", "Malaysia, Philippines, India"),
      new P("Malaysia Philippines,India,", "Malaysia, Philippines, India"),
    };

    [TestCaseSource(nameof(parsingPositiveValues))]
    public void ParsingPositive(P p)
    {
      IncludeCountryFilter filter = new IncludeCountryFilter();
      filter.Countries = p.Actual;

      Assert.AreEqual(p.Expected, filter.Countries);
    }

    public class U
    {
      public readonly string Filter;
      public readonly string Value;
      public readonly bool Expected;

      public U(string filter, string value, bool expected)
      {
        Filter = filter;
        Value = value;
        Expected = expected;
      }

      public override string ToString()
      {
        return $"Filter: '{Filter}', Value: '{Value}', Expected: {(Expected ? "Pass" : "Fail")}";
      }
    }

    public static readonly U[] includeFilterValues = new U[]
    {
      new U("Malaysia", null, false), 

      new U("Malaysia", "Malaysia", true), 
      new U("Malaysia", "malaysia", true), 

      new U("Malaysia, Philippines", "Malaysia", true), 

      new U("Malaysia, Philippines", "India", false), 
    };

    [TestCaseSource(nameof(includeFilterValues))]
    public void IncludeFilter(U u)
    {
      ProxyInformation proxy = new ProxyInformation("192.168.0.1", 80, "http", true, null, null, u.Value);

      IncludeCountryFilter filter = new IncludeCountryFilter();
      filter.Countries = u.Filter;

      Assert.False(filter.Validate().GetEnumerator().MoveNext());

      Assert.AreEqual(u.Expected, filter.DoesPassFilter(proxy));
    }

    public static readonly U[] excludeFilterValues = new U[]
    {
      new U("Malaysia", null, true), 

      new U("Malaysia", "Malaysia", false), 
      new U("Malaysia", "malaysia", false), 

      new U("Malaysia, Philippines", "Malaysia", false), 

      new U("Malaysia, Philippines", "India", true), 
    };

    [TestCaseSource(nameof(excludeFilterValues))]
    public void ExcludeFilter(U u)
    {
      ProxyInformation proxy = new ProxyInformation("192.168.0.1", 80, "http", true, null, null, u.Value);

      ExcludeCountryFilter filter = new ExcludeCountryFilter();
      filter.Countries = u.Filter;

      Assert.False(filter.Validate().GetEnumerator().MoveNext());

      Assert.AreEqual(u.Expected, filter.DoesPassFilter(proxy));
    }
  }
}
