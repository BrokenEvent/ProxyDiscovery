using BrokenEvent.ProxyDiscovery.Helpers;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class HttpResponseParserTests
  {
    public class U
    {
      public readonly string Response;
      public readonly bool IsValid;
      public readonly int Code;
      public readonly string Phrase;

      public U(string response, bool isValid, int code, string phrase)
      {
        Response = response;
        IsValid = isValid;
        Code = code;
        Phrase = phrase;
      }

      public override string ToString()
      {
        return $"{Response} → {Code} {Phrase} {(IsValid ? "valid" : "invalid")}";
      }
    }

    public static readonly U[] testData = new U[]
    {
      new U("HTTP/1.0 200 OK\r\n\r\n" , true, 200, "OK"), 
      new U("HTTP/1.0  200 Connection established\r\n\r\n" , true, 200, "Connection established"), 
      new U("HTTP/1.0 256\r\n\r\n" , true, 256, null), 
      new U("HjTP/1.1 200\r\n\r\n" , false, 200, null), 
      new U("HTTP/1.0 OK\r\n\r\n" , false, 200, null), 
      new U("Some wrong lorem ipsum" , false, 200, null), 
    };

    [TestCaseSource(nameof(testData))]
    public void TestResponseParser(U u)
    {
      HttpResponseParser parser = new HttpResponseParser(u.Response);

      if (!u.IsValid)
      {
        Assert.False(parser.IsValid);
        return;
      }

      Assert.IsTrue(parser.IsValid);
      Assert.AreEqual(u.Code, parser.StatusCode);
      Assert.AreEqual(u.Phrase, parser.Phrase);
    }
  }
}
