namespace BankSystem.Domain.Exceptions;

public class BankingException : Exception
{
    public BankingException(string message) : base(message) { }
    public BankingException(string message, Exception inner) : base(message, inner) { }
}

public class InsufficientFundsException : BankingException
{
    public InsufficientFundsException(string message) : base(message) { }
}

public class AccountNotFoundException : BankingException
{
    public Guid AccountId { get; }

    public AccountNotFoundException(Guid id)
        : base($"Account {id} was not found.") => AccountId = id;
}

public class DepositNotMaturedException : BankingException
{
    public DepositNotMaturedException(Guid depositId)
        : base($"Deposit {depositId} has not reached maturity yet.") { }
}

public class InvalidTransactionException : BankingException
{
    public InvalidTransactionException(string message) : base(message) { }
}
