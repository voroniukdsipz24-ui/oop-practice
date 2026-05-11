using BankSystem.Domain.Strategies;
using BankSystem.Domain.Entities;
using BankSystem.Domain.Interfaces;

namespace BankSystem.Application.Factories;

/// <summary>Factory Method pattern — encapsulates account creation logic.</summary>
public static class AccountFactory
{
    public static BankAccount CreateChecking(string owner, decimal initial, decimal overdraft = 0) =>
        new CheckingAccount(owner, initial, overdraft);

    public static BankAccount CreateSavings(
        string owner, decimal initial, string strategyType = "compound", decimal rate = 0.05m) =>
        new SavingsAccount(owner, initial, CreateStrategy(strategyType, rate));

    /// <summary>Strategy factory — selects algorithm by string key (e.g., from JSON config or CLI).</summary>
    public static IInterestStrategy CreateStrategy(string type, decimal rate) =>
        type.ToLowerInvariant() switch
        {
            "simple" => new SimpleInterestStrategy(rate),
            "compound" => new CompoundInterestStrategy(rate),
            "tiered" => new TieredInterestStrategy(new[]
            {
                (0m, 0.01m), (1000m, 0.03m), (10000m, rate)
            }),
            _ => throw new ArgumentException($"Unknown strategy type: {type}")
        };
}

/// <summary>Singleton — single application-wide audit logger.</summary>
public sealed class AuditLogger
{
    private static readonly Lazy<AuditLogger> _instance = new(() => new AuditLogger());
    private readonly List<string> _entries = new();
    private readonly object _lock = new();

    public static AuditLogger Instance => _instance.Value;

    public IReadOnlyList<string> Entries
    {
        get { lock (_lock) return _entries.ToList().AsReadOnly(); }
    }

    private AuditLogger() { }

    public void Log(string message)
    {
        lock (_lock)
            _entries.Add($"[{DateTime.UtcNow:O}] {message}");
    }

    public void Clear()
    {
        lock (_lock) _entries.Clear();
    }
}
