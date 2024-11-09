using System.Collections.Generic;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  public class LogCollector
  {
    public List<string> Errors { get; } = new List<string>();

    public void AddLog(string s)
    {
      Errors.Add(s);
    }
  }
}
