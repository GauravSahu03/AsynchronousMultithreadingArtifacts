using System.Diagnostics;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("--- .NET Asynchronous and Multithreading Demo Artifact#2 ---");

        // DEMO 1: The Asynchronous Chef (async/await for Concurrent I/O)
        Console.WriteLine("\n--- DEMO 1: The Asynchronous Chef ---");
        await RunAsynchronousChefDemoAsync();


        // DEMO 2: The Hard-Working Counter (TPL for CPU-Bound Work)
        Console.WriteLine("\n--- DEMO 2: The Hard-Working Counter ---");
        await RunCpuBoundCounterDemoAsync();


        // DEMO 3: The Shared Tip Jar - Problem (Race Condition)
        Console.WriteLine("\n--- DEMO 3: The Shared Tip Jar (Race Condition) ---");
        await RunRaceConditionDemoAsync();


        // DEMO 4: The Shared Tip Jar - Solution (lock)
        Console.WriteLine("\n--- DEMO 4: The Shared Tip Jar (Fixed with 'lock') ---");
        await RunLockingDemoAsync();

        
        // DEMO 5: The Coffee Shop (Throttling with SemaphoreSlim)
        Console.WriteLine("\n--- DEMO 5: The Coffee Shop with Limited Machines ---");
        await RunSemaphoreSlimDemoAsync();
    }

    #region DEMO 1: The Asynchronous Chef

    private static async Task RunAsynchronousChefDemoAsync()
    {
        Console.WriteLine("Chef is starting to make a breakfast of coffee, eggs, and toast.");
        var stopwatch = Stopwatch.StartNew();

        // A good chef works concurrently. They start tasks that can run on their own
        // (like toasting bread or brewing coffee) and don't just stand there waiting.
        var coffeeTask = MakeCoffeeAsync();
        var eggsTask = FryEggsAsync();
        var toastTask = MakeToastAsync();

        // Using Task.WhenAll, the chef waits for all the concurrent tasks to finish
        // before serving the breakfast. The main thread is not blocked.
        await Task.WhenAll(coffeeTask, eggsTask, toastTask);

        stopwatch.Stop();
        Console.WriteLine($"Breakfast is ready in {stopwatch.ElapsedMilliseconds}ms!");
    }

    private static async Task MakeCoffeeAsync()
    {
        Console.WriteLine("Starting the coffee machine...");
        // 'await Task.Delay' simulates an I/O-bound operation, like waiting for
        // a coffee machine to brew. The thread is released during this wait.
        await Task.Delay(3000); // 3 seconds to brew
        Console.WriteLine("Coffee is ready!");
    }

    private static async Task FryEggsAsync()
    {
        Console.WriteLine("Putting eggs in the pan...");
        await Task.Delay(2000); // 2 seconds to fry
        Console.WriteLine("Eggs are ready!");
    }

    private static async Task MakeToastAsync()
    {
        Console.WriteLine("Putting bread in the toaster...");
        await Task.Delay(2500); // 2.5 seconds to toast
        Console.WriteLine("Toast is ready!");
    }

    #endregion

    #region DEMO 2: The Hard-Working Counter (CPU-Bound Analogy)

    private static async Task RunCpuBoundCounterDemoAsync()
    {
        Console.WriteLine("Main thread: I need to count to a very large number, but I don't want to freeze.");
        Console.WriteLine("Main thread: I'll ask a helper thread from the Thread Pool to do it for me.");
        var stopwatch = Stopwatch.StartNew();

        // Task.Run offloads the CPU-intensive 'CountToABillion' method to a background thread.
        // The main thread is free to continue doing other work immediately.
        long result = await Task.Run(() => CountToABillion());

        stopwatch.Stop();
        Console.WriteLine($"Helper thread finished counting in {stopwatch.ElapsedMilliseconds}ms.");
        Console.WriteLine($"Main thread: The final count is {result:N0}. I was responsive the whole time!");
    }

    private static long CountToABillion()
    {
        Console.WriteLine("   Helper thread: Starting to count... this will take a moment.");
        long total = 0;
        for (int i = 0; i < 1_000_000_000; i++)
        {
            total++;
        }
        Console.WriteLine("   Helper thread: I'm done counting!");
        return total;
    }

    #endregion

    #region DEMO 3 & 4: The Shared Tip Jar (Synchronization)

    private static int _sharedTipJar;
    private static readonly object _tipJarLock = new();

    private static async Task RunRaceConditionDemoAsync()
    {
        _sharedTipJar = 0;
        Console.WriteLine("Two waiters are adding tips to a shared jar. Each waiter gets 1,000,000 tips.");

        // We create two tasks representing two waiters working at the same time.
        var waiter1 = Task.Run(() => {
            for (int i = 0; i < 1_000_000; i++)
            {
                // This is the race condition. Adding a tip isn't one single action.
                // It's
                // 1. Read current total,
                // 2. Add 1,
                // 3. Write new total.
                // The waiters can interrupt each other, causing tips to be lost.
                _sharedTipJar++;
            }
        });

        var waiter2 = Task.Run(() => {
            for (int i = 0; i < 1_000_000; i++)
            {
                _sharedTipJar++;
            }
        });

        await Task.WhenAll(waiter1, waiter2);

        Console.WriteLine($"Expected final tips: 2,000,000");
        Console.WriteLine($"Actual tips in jar:  {_sharedTipJar:N0}");
        Console.WriteLine("The total is wrong because some tips were lost!");
    }

    private static async Task RunLockingDemoAsync()
    {
        _sharedTipJar = 0;
        Console.WriteLine("The waiters will now use a lock, so only one can access the jar at a time.");

        var waiter1 = Task.Run(() => {
            for (int i = 0; i < 1_000_000; i++)
            {
                // The 'lock' ensures that the code inside is a "critical section".
                // Waiter 2 cannot start adding a tip until Waiter 1 is finished.
                // This makes the operation atomic and prevents lost updates.
                lock (_tipJarLock)
                {
                    _sharedTipJar++;
                }
            }
        });

        var waiter2 = Task.Run(() => {
            for (int i = 0; i < 1_000_000; i++)
            {
                lock (_tipJarLock)
                {
                    _sharedTipJar++;
                }
            }
        });

        await Task.WhenAll(waiter1, waiter2);

        Console.WriteLine($"Expected final tips: 2,000,000");
        Console.WriteLine($"Actual tips in jar:  {_sharedTipJar:N0}");
        Console.WriteLine("The total is now correct!");
    }

    #endregion

    #region DEMO 5: The Coffee Shop (SemaphoreSlim for Throttling)

    // A coffee shop has only 2 espresso machines.
    // The SemaphoreSlim will ensure only 2 "customers" (tasks) can use a machine at once.
    private static readonly SemaphoreSlim _espressoMachines = new(initialCount: 2);

    private static async Task RunSemaphoreSlimDemoAsync()
    {
        Console.WriteLine("A rush of 8 customers arrives at the coffee shop, which has 2 machines.");
        var stopwatch = Stopwatch.StartNew();

        var customerTasks = new List<Task>();
        for (int i = 1; i <= 8; i++)
        {
            customerTasks.Add(ServeCustomerAsync(i));
        }

        await Task.WhenAll(customerTasks);
        stopwatch.Stop();
        Console.WriteLine($"All 8 customers have been served in {stopwatch.ElapsedMilliseconds}ms.");
    }

    private static async Task ServeCustomerAsync(int customerId)
    {
        Console.WriteLine($"Customer {customerId} is waiting for an espresso machine.");

        // Asynchronously wait to acquire a "machine". If both are busy,
        // this task waits here until another customer calls Release().
        await _espressoMachines.WaitAsync();

        try
        {
            Console.WriteLine($"---> Customer {customerId} is using a machine.");
            await Task.Delay(2000); // Simulate the time it takes to make a coffee
            Console.WriteLine($"<--- Customer {customerId} is done and leaves.");
        }
        finally
        {
            // The customer releases the machine so someone else in line can use it.
            // This is critical to put in a 'finally' block.
            _espressoMachines.Release();
        }
    }

    #endregion
}
