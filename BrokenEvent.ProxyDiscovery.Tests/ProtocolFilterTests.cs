using BrokenEvent.ProxyDiscovery.Filters;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class ProtocolFilterTests
  {
    [Test]
    public void Positive()
    {
      ProxyInformation proxy = new ProxyInformation("192.168.0.1", 80, protocol: "http");

      ProtocolFilter filter = new ProtocolFilter { Protocol = "http" };

      Assert.False(filter.Validate().GetEnumerator().MoveNext());
      Assert.True(filter.DoesPassFilter(proxy));
    }

    [Test]
    public void Negative()
    {
      ProxyInformation proxy = new ProxyInformation("192.168.0.1", 80, protocol: "socks4");

      ProtocolFilter filter = new ProtocolFilter { Protocol = "http" };

      Assert.False(filter.Validate().GetEnumerator().MoveNext());
      Assert.IsFalse(filter.DoesPassFilter(proxy));
    }

    [Test]
    public void ValidationError()
    {
      ProtocolFilter filter = new ProtocolFilter();

      Assert.IsTrue(filter.Validate().GetEnumerator().MoveNext());
    }
  }
}
