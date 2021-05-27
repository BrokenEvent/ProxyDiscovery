using System;

namespace BrokenEvent.ProxyDiscovery.Helpers
{
  static class StringHelpers
  {
    public static bool ParseBool(string s)
    {
      if (string.IsNullOrWhiteSpace(s))
        return false;

      s = s.ToLower();
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
  }
}
