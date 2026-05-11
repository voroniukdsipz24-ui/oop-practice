using BankSystem.Domain.Interfaces;

namespace BankSystem.Domain.Entities;

public sealed class CheckingAccount : BankAccount
{
    private decimal _overdraftLimit;

    public decimal OverdraftLimit
    {
        get => _overdraftLimit;
        set
        {
            if (value < 0)
                throw new ArgumentException("Overdraft limit cannot be negative.");
            _overdraftLimit = value;
        }
    }

    public CheckingAccount(string owner, decimal initialBalance, decimal overdraftLimit = 0)
        : base(owner, initialBalance)
    {
        OverdraftLimit = overdraftLimit;
    }

    protected override decimal GetMinAllowedBalance() => -OverdraftLimit;

    public override string ToString() =>
        base.ToString() + $" | Overdraft: {OverdraftLimit:C}";
}

public sealed class SavingsAccount : BankAccount
{
    private IInterestStrategy _interestStrategy;

    public IInterestStrategy InterestStrategy
    {
        get => _interestStrategy;
        set => _interestStrategy = value ?? throw new ArgumentNullException(nameof(value));
    }

    public SavingsAccount(string owner, decimal initialBalance, IInterestStrategy interestStrategy)
        : base(owner, initialBalance)
    {
        _interestStrategy = interestStrategy ?? throw new ArgumentNullException(nameof(interestStrategy));
    }

    /// <summary>Applies accrued interest to the balance.</summary>
    public void ApplyInterest()
    {
        var interest = _interestStrategy.CalculateInterest(Balance);
        Deposit(interest);
    }

    public override string ToString() =>
        base.ToString() + $" | Strategy: {_interestStrategy.GetType().Name}";
}
