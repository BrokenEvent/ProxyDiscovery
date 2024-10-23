using System;
using System.Collections.Generic;

using BrokenEvent.ProxyDiscovery.Helpers;

using NUnit.Framework;

namespace BrokenEvent.ProxyDiscovery.Tests
{
  [TestFixture]
  class HelpersTests
  {
    public class U
    {
      public List<int> List { get; }

      public U(params int[] list)
      {
        List = new List<int>(list);
      }

      public override string ToString()
      {
        return string.Join(", ", List);
      }
    }

    public static readonly U[] randomizeListValues = new U[]
    {
      new U(1, 2, 3, 4, 5),
      new U(1, 2, 3, 4),
      new U(1, 2, 3),
      new U(1, 2),
      new U()
    };

    [TestCaseSource(nameof(randomizeListValues))]
    public void RandomizeList(U u)
    {
      IList<int> randomized = ((IEnumerable<int>)u.List).Randomize(new Random(1));

      Assert.AreEqual(u.List.Count, randomized.Count);

      for (int i = 0; i < u.List.Count; i++)
        Assert.AreNotEqual(u.List[i], randomized[i]);
    }
  }
}
