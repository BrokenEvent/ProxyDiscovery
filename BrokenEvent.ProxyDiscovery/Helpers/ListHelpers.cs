using System;
using System.Collections.Generic;

namespace BrokenEvent.ProxyDiscovery.Helpers
{
  public static class ListHelpers
  {
    public static void Randomize<T>(this IList<T> list, Random random = null)
    {
      if (random == null)
        random = new Random();

      // https://stackoverflow.com/a/1262619/4588884

      int n = list.Count;
      while (n > 1)
      {
        n--;
        int k = random.Next(n + 1);

        T value = list[k];
        list[k] = list[n];
        list[n] = value;
      }
    }

    public static IList<T> Randomize<T>(this IEnumerable<T> source, Random random = null)
    {
      List<T> list = new List<T>(source);
      list.Randomize(random);
      return list;
    }
  }
}
