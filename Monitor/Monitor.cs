using System;
using System.Threading;
using System.Threading.Tasks;

class BankAccount
{
    private object lockObj = new();
    public int Balance { get; private set; }

    public BankAccount(int initialBalance)
    {
        Balance = initialBalance;
    }

    public bool Withdraw(int amount)
    {
        lock (lockObj)
        {
            if (Balance >= amount)
            {
                Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Withdrawing {amount}...");
                Balance -= amount;
                Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] New balance: {Balance}");
                return true;
            }
            else
            {
                Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Withdrawal of {amount} failed. Insufficient funds.");
                return false;
            }
        }
    }
}

class Program
{
    static void Main()
    {
        var account = new BankAccount(500);

        var tasks = new Task[5];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 2; j++) // Each thread tries 2 withdrawals
                {
                    account.Withdraw(100);
                    Thread.Sleep(100); // Simulate time between transactions
                }
            });
        }

        Task.WaitAll(tasks);

        Console.WriteLine($"✅ Final account balance: {account.Balance}");
    }
}
