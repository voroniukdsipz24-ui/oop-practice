namespace BankSystem.Domain.Entities;

public enum TransactionType { Deposit, Withdrawal, Interest }

public sealed class Transaction
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid AccountId { get; }
    public decimal Amount { get; }
    public TransactionType Type { get; }
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    public Transaction(Guid accountId, decimal amount, TransactionType type)
    {
        AccountId = accountId;
        Amount = amount;
        Type = type;
    }

    public override string ToString() =>
        $"{Timestamp:yyyy-MM-dd HH:mm:ss} | {Type,-12} | {Amount,10:C}";
}

public sealed class Deposit
{
    private decimal _rate;

    public Guid Id { get; } = Guid.NewGuid();
    public Guid AccountId { get; }
    public decimal Principal { get; }
    public DateTime OpenedAt { get; } = DateTime.UtcNow;
    public DateTime MaturityDate { get; }

    public decimal Rate
    {
        get => _rate;
        set
        {
            if (value is < 0 or > 1)
                throw new ArgumentException("Rate must be between 0 and 1.");
            _rate = value;
        }
    }

    public bool IsMatured => DateTime.UtcNow >= MaturityDate;

    public decimal CalculateReturn() =>
        Principal * (1 + Rate * (decimal)(MaturityDate - OpenedAt).TotalDays / 365);

    public Deposit(Guid accountId, decimal principal, decimal rate, DateTime maturityDate)
    {
        if (principal <= 0) throw new ArgumentException("Principal must be positive.");
        AccountId = accountId;
        Principal = principal;
        Rate = rate;
        MaturityDate = maturityDate;
    }

    public override string ToString() =>
        $"Deposit {Id} | Principal: {Principal:C} | Rate: {Rate:P} | Matures: {MaturityDate:d} | Matured: {IsMatured}";
}
