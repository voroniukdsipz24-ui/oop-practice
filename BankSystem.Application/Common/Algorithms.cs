using BankSystem.Application.Factories;
using BankSystem.Domain.Entities;

namespace BankSystem.Application.Common;

// Custom delegate (Practical 5 / 11)
public delegate bool TransactionFilter(Transaction tx);

/// <summary>Generic algorithms over collections — built on Func/Action delegates.</summary>
public static class CollectionAlgorithms
{
    public static void ForEach<T>(IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source) action(item);
    }

    public static IEnumerable<TResult> Map<TSource, TResult>(
        IEnumerable<TSource> source, Func<TSource, TResult> selector)
    {
        foreach (var item in source) yield return selector(item);
    }

    public static TAccumulate Reduce<TSource, TAccumulate>(
        IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> reducer)
    {
        var result = seed;
        foreach (var item in source) result = reducer(result, item);
        return result;
    }
}

/// <summary>Extension methods for BankAccount collections (Self-study 7).</summary>
public static class AccountExtensions
{
    public static decimal TotalBalance(this IEnumerable<BankAccount> accounts) =>
        accounts.Sum(a => a.Balance);

    public static IEnumerable<BankAccount> Wealthy(this IEnumerable<BankAccount> accounts, decimal threshold) =>
        accounts.Where(a => a.Balance >= threshold);

    public static IEnumerable<IGrouping<string, BankAccount>> GroupByOwner(
        this IEnumerable<BankAccount> accounts) =>
        accounts.GroupBy(a => a.Owner);

    public static IEnumerable<Transaction> RecentTransactions(
        this BankAccount account, TimeSpan window) =>
        account.Transactions.Where(t => DateTime.UtcNow - t.Timestamp <= window);
}

/// <summary>Retry policy with exponential backoff (Self-study 8).</summary>
public static class RetryPolicy
{
    public static T Execute<T>(Func<T> operation, int maxAttempts = 3, int baseDelayMs = 100)
    {
        Exception? lastException = null;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                lastException = ex;
                AuditLogger.Instance.Log($"Attempt {attempt}/{maxAttempts} failed: {ex.Message}");

                if (attempt < maxAttempts)
                {
                    int delay = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    Thread.Sleep(delay);
                }
            }
        }
        throw new InvalidOperationException(
            $"Operation failed after {maxAttempts} attempts.", lastException);
    }

    public static void Execute(Action operation, int maxAttempts = 3, int baseDelayMs = 100) =>
        Execute<object?>(() => { operation(); return null; }, maxAttempts, baseDelayMs);
}
