class Program
{
    static async Task Main()
    {
        await RunParallelJobsExample();
    }
 
    static async Task RunParallelJobsExample()
    {
        var semaphore = new SemaphoreSlim(2);
        async Task RunJobAsync(int id)
        {
            Console.WriteLine($"Job {id} queued...");
            await semaphore.WaitAsync();
            try
            {
                Console.WriteLine($"Job {id} started.");
                await Task.Delay(600);
                Console.WriteLine($"Job {id} finished.");
            }
            finally
            {
                semaphore.Release();
            }
        }

        var tasks = new Task[4];
        for (int i = 0; i < 4; i++)
            tasks[i] = RunJobAsync(i);
        await Task.WhenAll(tasks);
    }
  }
