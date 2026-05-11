using System.Collections.ObjectModel;
using System.Windows;
using BankSystem.Domain.Entities;
using BankSystem.Domain.Interfaces;

namespace BankSystem.Wpf.ViewModels;

public sealed class UiNotificationObserver : IAccountObserver
{
    public ObservableCollection<string> Messages { get; } = new();

    public void OnAccountChanged(BankAccount account, Transaction tx)
    {
        string typeName = tx.Type switch
        {
            TransactionType.Deposit => "Поповнення",
            TransactionType.Withdrawal => "Зняття",
            TransactionType.Interest => "Відсотки",
            _ => tx.Type.ToString()
        };

        var message =
            $"[{tx.Timestamp:HH:mm:ss}] {account.Owner}: {typeName} {tx.Amount:C} → {account.Balance:C}";

        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            Messages.Insert(0, message);
            if (Messages.Count > 100) Messages.RemoveAt(Messages.Count - 1);
        });
    }
}
