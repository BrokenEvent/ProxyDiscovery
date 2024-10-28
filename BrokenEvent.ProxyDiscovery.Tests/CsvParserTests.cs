using BrokenEvent.ProxyDiscovery.Parsers;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class CsvParserTests
  {
    public class U
    {
      public string Input { get;  }
      public string[] Expected { get; }

      public U(string input, string[] expected)
      {
        Input = input;
        Expected = expected;
      }

      public override string ToString()
      {
        return $"{Input} → [{string.Join(", ", Expected)}]";
      }
    }

    public static readonly U[] data = new U[]
    {
      new U("a,b,c", new string[] { "a", "b", "c" }),
      new U("a,b,c,", new string[] { "a", "b", "c", null }),
      new U("a,b\\,,c", new string[] { "a", "b,", "c" }),
      new U("a,\"b,\",c", new string[] { "a", "b,", "c" }),
      new U("a,\'b,\',c", new string[] { "a", "b,", "c" }),
      new U("a ,b , c ", new string[] { "a", "b", "c" }),
      new U("   a ,b , c\t", new string[] { "a", "b", "c" }),
      new U("a,\" b \",c", new string[] { "a", " b ", "c" }),
      new U("a,\"b \" ,c", new string[] { "a", "b \"", "c" }),
    };

    [TestCaseSource(nameof(data))]
    public void Test(U u)
    {
      CollectionAssert.AreEqual(u.Expected, CsvParser.Split(u.Input, ','));
    }
  }
}
