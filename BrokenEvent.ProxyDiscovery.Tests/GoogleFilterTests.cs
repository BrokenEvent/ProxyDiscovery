﻿using BrokenEvent.ProxyDiscovery.Filters;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class GoogleFilterTests
  {
    public class U
    {
      public bool Expected { get; }
      public ProxyInformation Proxy { get; }
      public bool AllowUnknown;

      public U(bool expected, bool? google, bool allowUnknown)
      {
        Expected = expected;
        Proxy = new ProxyInformation("192.168.0.1", 80, "http", isGooglePassed: google);
        AllowUnknown = allowUnknown;
      }

      public override string ToString()
      {
        return $"Allow unknown: {AllowUnknown}, Value: {Proxy.IsGooglePassed} → {Expected}";
      }
    }

    public static readonly U[] testData = new U[]
    {
      new U(true, true, true),
      new U(false, false, true),
      new U(true, null, true),

      new U(true, true, false),
      new U(false, false, false),
      new U(false, null, false),
    };

    [TestCaseSource(nameof(testData))]
    public void TestHttpsFilter(U u)
    {
      GooglePassedFilter filter = new GooglePassedFilter { AllowUnknown = u.AllowUnknown };

      Assert.False(filter.Validate().GetEnumerator().MoveNext());
      Assert.AreEqual(u.Expected, filter.DoesPassFilter(u.Proxy));
    }
  }
}
