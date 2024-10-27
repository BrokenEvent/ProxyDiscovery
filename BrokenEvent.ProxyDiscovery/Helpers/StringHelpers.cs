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

    public static string GetDigits(string s, ref int i)
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

      return i > start ? s.Substring(start, i - start) : null;
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

    public static bool CompareSubstring(string s, int i, string target)
    {
      for (int j = 0; j < target.Length; j++)
      {
        if (s[i + j] != target[i])
          return false;
      }

      return true;
    }

    public static void AppendItem(this StringBuilder sb, string value)
    {
      if (sb.Length > 0)
        sb.Append(", ");
      sb.Append(value);
    }

    public static void AppendItem(this StringBuilder sb, string name, string value)
    {
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
  }
}
