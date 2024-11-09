using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class CountryResolverTests
  {
    public class U
    {
      public string Input { get; }
      public string Expected { get; }

      public U(string input, string expected)
      {
        Input = input;
        Expected = expected;
      }

      public override string ToString()
      {
        return $"'{Input}' → '{Expected}'";
      }
    }

    public static readonly U[] countryValues = new U[]
    {
      new U("BJ", "Benin"), 
      new U("", null),
      new U("  ", null),
      new U(null, null),
      new U("Benin", "Benin"),
      new U("12", "12"),
    };

    [TestCaseSource(nameof(countryValues))]
    public void ResolveCountry(U u)
    {
      Assert.AreEqual(u.Expected, CountryResolver.Resolve(u.Input));
    }

    [TestCaseSource(nameof(countryValues))]
    public void ResolveCountryProxyInformation(U u)
    {
      ProxyInformation info = new ProxyInformation("192.168.0.1", 3128, country: u.Input);
      Assert.AreEqual(u.Expected, info.Country);
    }
  }
}
