using BrokenEvent.ProxyDiscovery.Filters;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class HttpsFilterTests
  {
    [Test]
    public void Positive()
    {
      ProxyInformation proxy = new ProxyInformation("192.168.0.1", 80, true);

      HttpsFilter filter = new HttpsFilter();
      
      Assert.False(filter.Validate().GetEnumerator().MoveNext());
      Assert.True(filter.DoesPassFilter(proxy));
    }

    [Test]
    public void Negative()
    {
      ProxyInformation proxy = new ProxyInformation("192.168.0.1", 80, false);

      HttpsFilter filter = new HttpsFilter();
      
      Assert.False(filter.Validate().GetEnumerator().MoveNext());
      Assert.False(filter.DoesPassFilter(proxy));
    }
  }
}
