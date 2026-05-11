using BankSystem.Domain.Exceptions;
using BankSystem.Domain.Interfaces;

namespace BankSystem.Domain.Entities;

public abstract class BankAccount
{
    private decimal _balance;
    private readonly List<IAccountObserver> _observers = new();
    private readonly List<Transaction> _transactions = new();

    public Guid Id { get; } = Guid.NewGuid();
    public string Owner { get; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public decimal Balance
    {
        get => _balance;
        protected set
        {
            if (value < GetMinAllowedBalance())
                throw new InsufficientFundsException($"Balance cannot fall below {GetMinAllowedBalance():C}.");
            _balance = value;
        }
    }

    public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();

    protected BankAccount(string owner, decimal initialBalance)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Owner name cannot be empty.", nameof(owner));
        if (initialBalance < 0)
            throw new ArgumentException("Initial balance cannot be negative.", nameof(initialBalance));

        Owner = owner;
        _balance = initialBalance;
    }

    public virtual void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Deposit amount must be positive.", nameof(amount));

        Balance += amount;
        var tx = new Transaction(Id, amount, TransactionType.Deposit);
        _transactions.Add(tx);
        NotifyObservers(tx);
    }

    public virtual void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive.", nameof(amount));

        Balance -= amount;
        var tx = new Transaction(Id, -amount, TransactionType.Withdrawal);
        _transactions.Add(tx);
        NotifyObservers(tx);
    }

    public void Subscribe(IAccountObserver observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }

    public void Unsubscribe(IAccountObserver observer) =>
        _observers.Remove(observer);

    protected virtual decimal GetMinAllowedBalance() => 0m;

    private void NotifyObservers(Transaction tx)
    {
        foreach (var observer in _observers)
            observer.OnAccountChanged(this, tx);
    }

    public override string ToString() =>
        $"[{GetType().Name}] Owner: {Owner} | Balance: {Balance:C} | Id: {Id}";

    // Operator overloads (Practical 2)
    public static bool operator ==(BankAccount? a, BankAccount? b) =>
        a?.Id == b?.Id;

    public static bool operator !=(BankAccount? a, BankAccount? b) =>
        !(a == b);

    public override bool Equals(object? obj) =>
        obj is BankAccount other && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();
}
