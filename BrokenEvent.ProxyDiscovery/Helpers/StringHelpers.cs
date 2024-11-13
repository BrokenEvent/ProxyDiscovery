using System;
using System.Net;
using System.Text;

namespace BrokenEvent.ProxyDiscovery.Helpers
{
  public static class StringHelpers
  {
    public static bool ParseBool(string s)
    {
      if (string.IsNullOrWhiteSpace(s))
        return false;

      s = s.Trim().ToLower();
      return s == "1" || s == "yes" || s == "true" || s == "+";
    }

    public static bool SkipSpaces(string s, ref int i)
    {
      while (i < s.Length)
      {
        if (s[i] != ' ')
          return true;

        i++;
      }

      return false;
    }

    public static bool SkipDelimiters(string s, ref int i)
    {
      while (i < s.Length)
      {
        char c = s[i];
        if (c != ' ' && c != ';' && c != ',')
          return true;

        i++;
      }

      return false;
    }

    public static int? GetDigits(string s, ref int i)
    {
      int start = i;

      if (!char.IsDigit(s, i))
        throw new FormatException($"Unexpected character: {s[i]} at {i}");

      while (i < s.Length)
      {
        if (!char.IsDigit(s, i))
          break;

        i++;
      }

      return i > start ? int.Parse(s.Substring(start, i - start)) : (int?)null;
    }

    public static string GetToken(string s, ref int i)
    {
      int start = i;

      while (i < s.Length)
      {
        char c = s[i];
        if (c == ' ' || c == ';' || c == ',')
          break;

        i++;
      }

      return i > start ? s.Substring(start, i - start) : null;
    }

    public static string ReadUntil(string s, ref int i, string target)
    {
      int index = s.IndexOf(target, i);

      if (index == i || index == -1)
        return null;

      string result = s.Substring(i, index - i);
      i = index + target.Length;
      return result;
    }

    public static void AppendItem(this StringBuilder sb, string value)
    {
      if (sb.Length > 0)
        sb.Append(", ");
      sb.Append(value);
    }

    public static void AppendItem(this StringBuilder sb, string name, string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return;
      if (sb.Length > 0)
        sb.Append(", ");
      sb.Append(name).Append(": ").Append(value);
    }

    public static bool ParseEndPoint(string value, out IPAddress address, out ushort port)
    {
      int i = value.IndexOf(':');
      if (i != -1)
      {
        address = IPAddress.Parse(value.Substring(0, i));

        return ushort.TryParse(value.Substring(i + 1), out port);
      }

      address = null;
      port = 0;
      return false;
    }

    public static string ProcessUrl(string url, int? pageNumber)
    {
      if (url == null)
        throw new ArgumentNullException(nameof(url));

      int startIndex = url.IndexOf('[');

      // no substitutions found
      if (startIndex == -1)
        return url;

      int endIndex = url.IndexOf(']', startIndex);

      // not terminated
      if (endIndex == -1)
        throw new FormatException("Unterminated substitution in URL. Expected to encounter ']'");

      StringBuilder sb = new StringBuilder();

      // part before substitution
      sb.Append(url, 0, startIndex);

      // substituted part
      if (pageNumber.HasValue)
        for (int i = startIndex + 1; i < endIndex; i++)
        {
          char c = url[i];
          if (c == '$')
            sb.Append(pageNumber.Value);
          else
            sb.Append(c);
        }

      // the endth part
      if (endIndex < url.Length - 1)
        sb.Append(url, endIndex + 1, url.Length - endIndex - 1);

      return sb.ToString();
    }

    public static bool CheckUrlForPageNumber(string url)
    {
      if (url == null)
        throw new ArgumentNullException(nameof(url));
      return url.IndexOf('[') != -1;
    }
  }
}
