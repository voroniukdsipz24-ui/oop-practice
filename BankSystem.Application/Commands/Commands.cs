using BankSystem.Domain.Entities;
using BankSystem.Domain.Interfaces;

namespace BankSystem.Application.Commands;

public sealed class DepositCommand : ICommand
{
    private readonly BankAccount _account;
    private readonly decimal _amount;

    public string Description => $"Deposit {_amount:C} to {_account.Owner}";

    public DepositCommand(BankAccount account, decimal amount)
    {
        _account = account ?? throw new ArgumentNullException(nameof(account));
        _amount = amount;
    }

    public void Execute() => _account.Deposit(_amount);
    public void Undo() => _account.Withdraw(_amount);
}

public sealed class WithdrawCommand : ICommand
{
    private readonly BankAccount _account;
    private readonly decimal _amount;

    public string Description => $"Withdraw {_amount:C} from {_account.Owner}";

    public WithdrawCommand(BankAccount account, decimal amount)
    {
        _account = account ?? throw new ArgumentNullException(nameof(account));
        _amount = amount;
    }

    public void Execute() => _account.Withdraw(_amount);
    public void Undo() => _account.Deposit(_amount);
}

public sealed class TransferCommand : ICommand
{
    private readonly BankAccount _from;
    private readonly BankAccount _to;
    private readonly decimal _amount;

    public string Description => $"Transfer {_amount:C} {_from.Owner} → {_to.Owner}";

    public TransferCommand(BankAccount from, BankAccount to, decimal amount)
    {
        _from = from ?? throw new ArgumentNullException(nameof(from));
        _to = to ?? throw new ArgumentNullException(nameof(to));
        if (from == to) throw new ArgumentException("Cannot transfer to the same account.");
        _amount = amount;
    }

    public void Execute()
    {
        _from.Withdraw(_amount);
        _to.Deposit(_amount);
    }

    public void Undo()
    {
        _to.Withdraw(_amount);
        _from.Deposit(_amount);
    }
}

/// <summary>Invoker — keeps history & supports undo/redo.</summary>
public sealed class CommandInvoker
{
    private readonly Stack<ICommand> _history = new();
    private readonly Stack<ICommand> _redoStack = new();

    public IEnumerable<ICommand> History => _history;

    public void Execute(ICommand command)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        command.Execute();
        _history.Push(command);
        _redoStack.Clear();
    }

    public bool UndoLast()
    {
        if (_history.Count == 0) return false;
        var cmd = _history.Pop();
        cmd.Undo();
        _redoStack.Push(cmd);
        return true;
    }

    public bool Redo()
    {
        if (_redoStack.Count == 0) return false;
        var cmd = _redoStack.Pop();
        cmd.Execute();
        _history.Push(cmd);
        return true;
    }
}
