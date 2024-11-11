namespace BrokenEvent.ProxyDiscovery.Interfaces
{
  public struct TestResult
  {
    public ProxyCheckResult Result;
    public string Message;

    public TestResult(ProxyCheckResult result, string message)
    {
      Result = result;
      Message = message;
    }
  }
}