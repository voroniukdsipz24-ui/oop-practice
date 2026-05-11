using BankSystem.Domain.Entities;
using BankSystem.Domain.Interfaces;

namespace BankSystem.Application.Observers;

public sealed class ConsoleLogObserver : IAccountObserver
{
    public void OnAccountChanged(BankAccount account, Transaction transaction)
    {
        Console.WriteLine(
            $"[LOG] {transaction.Timestamp:HH:mm:ss} | {account.Owner} | " +
            $"{transaction.Type}: {transaction.Amount:C} | New balance: {account.Balance:C}");
    }
}

public sealed class EmailNotifier : IAccountObserver
{
    private readonly string _email;
    private readonly decimal _largeAmountThreshold;

    public EmailNotifier(string email, decimal largeAmountThreshold = 1000m)
    {
        _email = email;
        _largeAmountThreshold = largeAmountThreshold;
    }

    public void OnAccountChanged(BankAccount account, Transaction transaction)
    {
        if (Math.Abs(transaction.Amount) >= _largeAmountThreshold)
        {
            Console.WriteLine(
                $"[EMAIL → {_email}] Large {transaction.Type} of {transaction.Amount:C} " +
                $"on account {account.Id}");
        }
    }
}

public sealed class SmsNotifier : IAccountObserver
{
    private readonly string _phone;

    public SmsNotifier(string phone) => _phone = phone;

    public void OnAccountChanged(BankAccount account, Transaction transaction) =>
        Console.WriteLine($"[SMS → {_phone}] {transaction.Type} {transaction.Amount:C}, balance {account.Balance:C}");
}
