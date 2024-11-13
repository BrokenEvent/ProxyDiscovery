using System;

using BrokenEvent.ProxyDiscovery.Helpers;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class StringHelperTests
  {
    public class U
    {
      public string ExpectedNull { get; }
      public string ExpectedNonNull { get; }
      public string Input { get; }
      public int Value { get; }

      public U(string input)
      {
        Input = input;
      }

      public U(string expectedNull, string expectedNonNull, string input, int value = 1)
      {
        ExpectedNull = expectedNull;
        ExpectedNonNull = expectedNonNull;
        Input = input;
        Value = value;
      }

      public override string ToString()
      {
        return $"{Input} → \"{ExpectedNull}\" / \"{ExpectedNonNull}\" ";
      }
    }

    public static readonly U[] processUrlData = new U[]
    {
      new U("http://brokenevent.com", "http://brokenevent.com", "http://brokenevent.com"), 
      new U("http://brokenevent.com/test", "http://brokenevent.com/test", "http://brokenevent.com/test"), 
      new U("http://brokenevent.com/test", "http://brokenevent.com/test/1", "http://brokenevent.com/test[/$]"), 
      new U("http://brokenevent.com/test", "http://brokenevent.com/test/1/", "http://brokenevent.com/test[/$/]"), 
      new U("http://brokenevent.com/test", "http://brokenevent.com/test1/", "http://brokenevent.com/test[$/]"), 
      new U("http://brokenevent.com/test", "http://brokenevent.com/test?page=10", "http://brokenevent.com/test[?page=$]", 10), 
      new U("http://brokenevent.com/test?id=0", "http://brokenevent.com/test?id=0&page=10", "http://brokenevent.com/test?id=0[&page=$]", 10), 
      new U("http://brokenevent.com/test?id=0", "http://brokenevent.com/test?page=10&id=0", "http://brokenevent.com/test?[page=$&]id=0", 10), 
      new U("http://brokenevent.com/test?id=0", "http://brokenevent.com/test?page=10&set=10&id=0", "http://brokenevent.com/test?[page=$&set=$&]id=0", 10), 
    };

    [TestCaseSource(nameof(processUrlData))]
    public void ProcessUrlTest(U u)
    {
      Assert.AreEqual(u.ExpectedNull, StringHelpers.ProcessUrl(u.Input, null));
      Assert.AreEqual(u.ExpectedNonNull, StringHelpers.ProcessUrl(u.Input, u.Value));
    }

    public static readonly U[] processUrlFormatExceptionsData = new U[]
    {
      new U("http://brokenevent.com["), 
      new U("http://brokenevent.com[/page=$"), 
      new U("http://brokenevent.com[/page="), 
      new U("http://brokenevent.com]/page=["), 
    };

    [TestCaseSource(nameof(processUrlFormatExceptionsData))]
    public void ProcessUrlFormatExceptionTest(U u)
    {
      Assert.Throws(typeof(FormatException), () => StringHelpers.ProcessUrl(u.Input, 10));
    }

    public class V
    {
      public string Input { get; }
      public bool Expected { get; }

      public V(string input, bool expected)
      {
        Input = input;
        Expected = expected;
      }

      public override string ToString()
      {
        return $"{Input} → {Expected}";
      }
    }

    public static readonly V[] checkUrlForPageNumberData = new V[]
    {
      new V("http://brokenevent.com", false), 
      new V("http://brokenevent.com/test", false), 
      new V("http://brokenevent.com/test[", true), 
      new V("http://brokenevent.com/test[/$]", true), 
    };

    [TestCaseSource(nameof(checkUrlForPageNumberData))]
    public void CheckUrlForPageNumberTest(V v)
    {
      Assert.AreEqual(v.Expected, StringHelpers.CheckUrlForPageNumber(v.Input));
    }
  }
}
