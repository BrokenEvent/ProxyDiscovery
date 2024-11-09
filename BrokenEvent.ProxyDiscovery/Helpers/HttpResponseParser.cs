using System.Text;

namespace BrokenEvent.ProxyDiscovery.Helpers
{
  public struct HttpResponseParser
  {
    public readonly string HttpVersion;
    public readonly int StatusCode;
    public readonly string Phrase;
    public readonly bool IsValid;

    public HttpResponseParser(byte[] bytes, int count)
      : this(
        Encoding.ASCII.GetString(bytes, 0, count)
      )
    {
      
    }

    public HttpResponseParser(string response)
      : this()
    {
      // HTTP/1.0 200 Connection established\r\n\r\n

      if (!response.StartsWith("HTTP/"))
        return;

      int i = 5;

      HttpVersion = StringHelpers.ReadUntil(response, ref i, " ");
      if (HttpVersion == null)
        return;

      StringHelpers.SkipSpaces(response, ref i);

      string code = StringHelpers.ReadUntil(response, ref i, " ");

      // the phrase may be skipped
      if (code == null)
      {
        code = StringHelpers.ReadUntil(response, ref i, "\r\n");
        if (code == null)
          return;
      }

      if (!int.TryParse(code, out StatusCode))
        return;

      Phrase = StringHelpers.ReadUntil(response, ref i, "\r\n");
      IsValid = true;
    }
  }
}