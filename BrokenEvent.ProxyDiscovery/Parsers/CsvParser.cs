using System.Collections.Generic;
using System.Text;

namespace BrokenEvent.ProxyDiscovery.Parsers
{
  public static class CsvParser
  {
    public static string GetCell(string line, ref int index, char separator)
    {
      char? startQuote = null;
      char? quoteChar = null;
      bool isEscape = false;
      StringBuilder sb = null;

      int startIndex = index;
      int nonWhitespaceCount = 0;

      while (index < line.Length)
      {
        char c = line[index];
        bool isWhitespace = false;

        // leave escaped character as is
        if (isEscape)
        {
          index++;
          isEscape = false;
          if (sb == null)
            sb = new StringBuilder();
          sb.Append(c);
          nonWhitespaceCount = sb.Length;
          continue;
        }

        if (!quoteChar.HasValue && c == separator)
          break; // we're done here

        switch (c)
        {
          // escape char
          case '\\':
            isEscape = true;
            index++;
            continue; // skip escaped character

          // quotes
          case '"':
          case '\'':
            if (quoteChar.HasValue)
            {
              if (quoteChar == c) // if the character is the same as begining - stop the quote mode
                quoteChar = null;
              break;
            }
            
            // only account if quote it first character in string and only once
            if (startIndex == index && !startQuote.HasValue)
            {
              startQuote = c; // remember it
              quoteChar = c; // enter quote mode
              index++; // and go ahead without adding it
              continue;
            }

            quoteChar = c; // start quote mode
            break;

          // whitespaces
          case ' ':
          case '\t':
            if (startIndex == index) // skip whitespaces at the beginning aka TrimStart()
            {
              startIndex++;
              index++;
              continue;
            }

            isWhitespace = true;
            break;
        }


        // regular character
        if (sb == null)
          sb = new StringBuilder();
        sb.Append(c);

        if (!isWhitespace)
          nonWhitespaceCount = sb.Length;

        index++;
      }

      if (sb == null)
        return null;

      // cut of trailing whitespaces aka TrimEnd()
      if (nonWhitespaceCount < sb.Length)
        sb.Length = nonWhitespaceCount;
      else if (startQuote.HasValue && startQuote.Value == sb[sb.Length - 1]) // if ends with the same quote as start - remove it.
        sb.Length--;

      return sb.ToString();
    }

    public static List<string> Split(string line, char separator)
    {
      List<string> list = new List<string>();

      int index = 0;

      while (true)
      {
        string cell = GetCell(line, ref index, separator);
        list.Add(cell);

        // are we at end?
        if (index >= line.Length)
          break;

        index++; // skip the separator itself

        // if we're now at end, we need to add trailing element too
        if (index >= line.Length)
        {
          list.Add(null);
          break;
        }
      }

      return list;
    }
  }
}
