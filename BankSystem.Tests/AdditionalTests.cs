using BankSystem.Application.Common;
using BankSystem.Application.Factories;
using BankSystem.Domain.Strategies;
using BankSystem.Domain.Entities;
using BankSystem.Infrastructure.Serialization;
using Xunit;

namespace BankSystem.Tests;

public class FactoryTests
{
    [Fact]
    public void Factory_CreatesCheckingAccount()
    {
        var acc = AccountFactory.CreateChecking("Alice", 100m, 50m);
        Assert.IsType<CheckingAccount>(acc);
        Assert.Equal(100m, acc.Balance);
    }

    [Theory]
    [InlineData("simple", typeof(SimpleInterestStrategy))]
    [InlineData("compound", typeof(CompoundInterestStrategy))]
    [InlineData("tiered", typeof(TieredInterestStrategy))]
    public void Factory_CreatesCorrectStrategyType(string key, Type expectedType)
    {
        var strat = AccountFactory.CreateStrategy(key, 0.05m);
        Assert.IsType(expectedType, strat);
    }

    [Fact]
    public void Factory_UnknownStrategyType_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AccountFactory.CreateStrategy("invalid", 0.05m));
    }

    [Fact]
    public void Singleton_AuditLogger_ReturnsSameInstance()
    {
        var a = AuditLogger.Instance;
        var b = AuditLogger.Instance;
        Assert.Same(a, b);
    }
}

public class CollectionAlgorithmsTests
{
    [Fact]
    public void ForEach_AppliesActionToAll()
    {
        var items = new[] { 1, 2, 3 };
        var sum = 0;
        CollectionAlgorithms.ForEach(items, x => sum += x);
        Assert.Equal(6, sum);
    }

    [Fact]
    public void Map_TransformsAllElements()
    {
        var result = CollectionAlgorithms.Map(new[] { 1, 2, 3 }, x => x * x).ToList();
        Assert.Equal(new[] { 1, 4, 9 }, result);
    }

    [Fact]
    public void Reduce_AggregatesCorrectly()
    {
        var result = CollectionAlgorithms.Reduce(
            new[] { 1, 2, 3, 4 }, 0, (acc, x) => acc + x);
        Assert.Equal(10, result);
    }
}

public class ExtensionMethodsTests
{
    [Fact]
    public void TotalBalance_SumsAllAccounts()
    {
        var accounts = new[]
        {
            new CheckingAccount("A", 100m),
            new CheckingAccount("B", 200m),
            new CheckingAccount("C", 300m)
        };
        Assert.Equal(600m, accounts.TotalBalance());
    }

    [Fact]
    public void Wealthy_FiltersByThreshold()
    {
        var accounts = new[]
        {
            new CheckingAccount("Poor", 100m),
            new CheckingAccount("Rich", 10000m)
        };
        var rich = accounts.Wealthy(1000m).ToList();
        Assert.Single(rich);
        Assert.Equal("Rich", rich[0].Owner);
    }

    [Fact]
    public void GroupByOwner_GroupsCorrectly()
    {
        var accounts = new BankAccount[]
        {
            new CheckingAccount("Alice", 100m),
            new CheckingAccount("Alice", 200m),
            new CheckingAccount("Bob", 300m)
        };
        var groups = accounts.GroupByOwner().ToList();
        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups.First(g => g.Key == "Alice").Count());
    }
}

public class RetryPolicyTests
{
    [Fact]
    public void Retry_SucceedsOnSecondAttempt()
    {
        int calls = 0;
        var result = RetryPolicy.Execute(() =>
        {
            calls++;
            if (calls < 2) throw new InvalidOperationException("transient");
            return "ok";
        }, maxAttempts: 3, baseDelayMs: 1);

        Assert.Equal("ok", result);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Retry_FailsAfterMaxAttempts()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RetryPolicy.Execute<int>(
                () => throw new InvalidOperationException("always"),
                maxAttempts: 2, baseDelayMs: 1));
    }
}

public class SerializationTests
{
    [Fact]
    public void Json_RoundTrip_PreservesData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = new[] { new CheckingAccount("Alice", 500m, 100m) };
            AccountSerializer.SaveToFile(original, path);
            var loaded = AccountSerializer.LoadFromFile(path).ToList();

            Assert.Single(loaded);
            Assert.Equal("Alice", loaded[0].Owner);
            Assert.Equal(500m, loaded[0].Balance);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Xml_RoundTrip_PreservesData()
    {
        var path = Path.GetTempFileName();
        try
        {
            var original = new[] { new CheckingAccount("Bob", 750m, 200m) };
            XmlAccountSerializer.SaveToFile(original, path);
            var loaded = XmlAccountSerializer.LoadFromFile(path).ToList();

            Assert.Single(loaded);
            Assert.Equal("Bob", loaded[0].Owner);
            Assert.Equal(750m, loaded[0].Balance);
        }
        finally { File.Delete(path); }
    }
}
