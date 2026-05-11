using BankSystem.Application.Commands;
using BankSystem.Domain.Strategies;
using BankSystem.Domain.Entities;
using BankSystem.Domain.Exceptions;
using BankSystem.Domain.Interfaces;
using Moq;
using Xunit;

namespace BankSystem.Tests;

public class BankAccountTests
{
    [Fact]
    public void Deposit_PositiveAmount_IncreasesBalance()
    {
        // Arrange
        var account = new CheckingAccount("Test", 100m);
        // Act
        account.Deposit(50m);
        // Assert
        Assert.Equal(150m, account.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Deposit_NonPositiveAmount_Throws(decimal amount)
    {
        var account = new CheckingAccount("Test", 100m);
        Assert.Throws<ArgumentException>(() => account.Deposit(amount));
    }

    [Fact]
    public void Withdraw_BeyondBalance_ThrowsInsufficientFunds()
    {
        var account = new CheckingAccount("Test", 100m);
        Assert.Throws<InsufficientFundsException>(() => account.Withdraw(200m));
    }

    [Fact]
    public void Withdraw_WithinOverdraft_Succeeds()
    {
        var account = new CheckingAccount("Test", 100m, overdraftLimit: 200m);
        account.Withdraw(250m);
        Assert.Equal(-150m, account.Balance);
    }

    [Fact]
    public void Equality_SameId_ReturnsTrue()
    {
        var a = new CheckingAccount("Alice", 100m);
        BankAccount b = a;
        Assert.True(a == b);
        Assert.True(a.Equals(b));
    }
}

public class StrategyTests
{
    [Fact]
    public void SimpleInterest_CalculatesFlatRate()
    {
        var strat = new SimpleInterestStrategy(0.10m);
        Assert.Equal(100m, strat.CalculateInterest(1000m));
    }

    [Fact]
    public void CompoundInterest_GreaterThanSimple()
    {
        var simple = new SimpleInterestStrategy(0.10m);
        var compound = new CompoundInterestStrategy(0.10m, 12);
        Assert.True(compound.CalculateInterest(1000m) > simple.CalculateInterest(1000m));
    }

    [Fact]
    public void TieredInterest_AppliesCorrectTier()
    {
        var strat = new TieredInterestStrategy(new[]
        {
            (0m, 0.01m),
            (1000m, 0.03m),
            (10000m, 0.05m)
        });
        Assert.Equal(50m, strat.CalculateInterest(1000m));
        Assert.Equal(500m, strat.CalculateInterest(10000m));
    }

    [Fact]
    public void Strategy_CanBeSwapped_AtRuntime()
    {
        var savings = new SavingsAccount("Test", 1000m, new SimpleInterestStrategy(0.05m));
        savings.InterestStrategy = new CompoundInterestStrategy(0.05m, 4);
        Assert.IsType<CompoundInterestStrategy>(savings.InterestStrategy);
    }
}

public class CommandTests
{
    [Fact]
    public void DepositCommand_ExecuteAndUndo_RestoresBalance()
    {
        var account = new CheckingAccount("Test", 100m);
        var cmd = new DepositCommand(account, 50m);

        cmd.Execute();
        Assert.Equal(150m, account.Balance);

        cmd.Undo();
        Assert.Equal(100m, account.Balance);
    }

    [Fact]
    public void TransferCommand_MovesFunds_AndUndoesCleanly()
    {
        var from = new CheckingAccount("From", 1000m);
        var to = new CheckingAccount("To", 0m);
        var cmd = new TransferCommand(from, to, 300m);

        cmd.Execute();
        Assert.Equal(700m, from.Balance);
        Assert.Equal(300m, to.Balance);

        cmd.Undo();
        Assert.Equal(1000m, from.Balance);
        Assert.Equal(0m, to.Balance);
    }

    [Fact]
    public void Invoker_UndoAndRedo_WorkInSequence()
    {
        var account = new CheckingAccount("Test", 100m);
        var invoker = new CommandInvoker();

        invoker.Execute(new DepositCommand(account, 100m));
        Assert.Equal(200m, account.Balance);

        Assert.True(invoker.UndoLast());
        Assert.Equal(100m, account.Balance);

        Assert.True(invoker.Redo());
        Assert.Equal(200m, account.Balance);
    }

    [Fact]
    public void Invoker_UndoOnEmpty_ReturnsFalse()
    {
        var invoker = new CommandInvoker();
        Assert.False(invoker.UndoLast());
    }
}

public class ObserverTests
{
    [Fact]
    public void Observer_NotifiedOnDeposit()
    {
        // Arrange — Mock observer (Self-study 14)
        var account = new CheckingAccount("Test", 100m);
        var mockObserver = new Mock<IAccountObserver>();
        account.Subscribe(mockObserver.Object);

        // Act
        account.Deposit(50m);

        // Assert
        mockObserver.Verify(
            o => o.OnAccountChanged(account, It.Is<Transaction>(
                t => t.Type == TransactionType.Deposit && t.Amount == 50m)),
            Times.Once);
    }

    [Fact]
    public void Observer_Unsubscribed_NoLongerNotified()
    {
        var account = new CheckingAccount("Test", 100m);
        var mockObserver = new Mock<IAccountObserver>();
        account.Subscribe(mockObserver.Object);
        account.Unsubscribe(mockObserver.Object);

        account.Deposit(50m);

        mockObserver.Verify(
            o => o.OnAccountChanged(It.IsAny<BankAccount>(), It.IsAny<Transaction>()),
            Times.Never);
    }

    [Fact]
    public void MultipleObservers_AllNotified()
    {
        var account = new CheckingAccount("Test", 100m);
        var m1 = new Mock<IAccountObserver>();
        var m2 = new Mock<IAccountObserver>();
        account.Subscribe(m1.Object);
        account.Subscribe(m2.Object);

        account.Withdraw(20m);

        m1.Verify(o => o.OnAccountChanged(It.IsAny<BankAccount>(), It.IsAny<Transaction>()), Times.Once);
        m2.Verify(o => o.OnAccountChanged(It.IsAny<BankAccount>(), It.IsAny<Transaction>()), Times.Once);
    }
}
