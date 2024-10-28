using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class ServiceDetectorTests
  {
    public class U
    {
      public ushort Port { get; }
      public string Expected { get; }

      public U(ushort port, string expected)
      {
        Port = port;
        Expected = expected;
      }

      public override string ToString()
      {
        return $"{Port} → {Expected}";
      }
    }

    public static readonly U[] detectorValues = new U[]
    {
      new U(9999, ServiceDetector.CustomResult),
      new U(8118, "Privoxy"),
      new U(8081, "Microsoft ISA/TMG or Apache Traffic Server"),
      new U(3128, "Squid, Blue Coat ProxySG or 3Proxy"),
      new U(80, "Nginx Proxy, WinGate, HAProxy or Varnish Cache"),
    };

    [TestCaseSource(nameof(detectorValues))]
    public void DetectService(U u)
    {
      Assert.AreEqual(u.Expected, ServiceDetector.DetectService(u.Port));
    }
  }
}
