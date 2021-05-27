using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BrokenEvent.ProxyDiscovery.Helpers
{
  public class TaskQueue<T>
  {
    private ConcurrentQueue<T> taskArgs;
    private Func<T, CancellationToken, Task< bool>> task;
    private int threadsCount;
    private CancellationToken ct;

    public TaskQueue(IEnumerable<T> args, Func<T, CancellationToken, Task<bool>> task, int threadsCount, CancellationToken ct)
    {
      this.threadsCount = threadsCount;
      this.ct = ct;
      this.task = task;
      taskArgs = new ConcurrentQueue<T>(args);
    }

    public Task Run()
    {
      List<Task> threads = new List<Task>();

      for (int i = 0; i < threadsCount; i++)
        threads.Add(Task.Run(ThreadRun, ct));

      return Task.WhenAll(threads);
    }

    private async Task ThreadRun()
    {
      T arg;
      while (taskArgs.TryDequeue(out arg))
      {
        if (!await task(arg, ct).ConfigureAwait(false))
          break;
      }
    }
  }
}
