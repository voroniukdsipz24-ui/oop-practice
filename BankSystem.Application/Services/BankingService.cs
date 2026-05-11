using BankSystem.Application.Commands;
using BankSystem.Domain.Entities;
using BankSystem.Domain.Exceptions;
using BankSystem.Domain.Interfaces;

namespace BankSystem.Application.Services;

/// <summary>Facade: simplifies access to the banking subsystems.</summary>
public sealed class BankingService
{
    private readonly IRepository<BankAccount> _accounts;
    private readonly CommandInvoker _invoker;

    public BankingService(IRepository<BankAccount> accounts, CommandInvoker invoker)
    {
        _accounts = accounts;
        _invoker = invoker;
    }

    public BankAccount OpenChecking(string owner, decimal initial, decimal overdraft = 0)
    {
        var acc = new CheckingAccount(owner, initial, overdraft);
        _accounts.Add(acc);
        return acc;
    }

    public BankAccount OpenSavings(string owner, decimal initial, IInterestStrategy strategy)
    {
        var acc = new SavingsAccount(owner, initial, strategy);
        _accounts.Add(acc);
        return acc;
    }

    public void Deposit(Guid accountId, decimal amount)
    {
        var acc = _accounts.GetById(accountId)
            ?? throw new AccountNotFoundException(accountId);
        _invoker.Execute(new DepositCommand(acc, amount));
    }

    public void Withdraw(Guid accountId, decimal amount)
    {
        var acc = _accounts.GetById(accountId)
            ?? throw new AccountNotFoundException(accountId);
        _invoker.Execute(new WithdrawCommand(acc, amount));
    }

    public void Transfer(Guid fromId, Guid toId, decimal amount)
    {
        var from = _accounts.GetById(fromId) ?? throw new AccountNotFoundException(fromId);
        var to = _accounts.GetById(toId) ?? throw new AccountNotFoundException(toId);
        _invoker.Execute(new TransferCommand(from, to, amount));
    }

    public bool UndoLast() => _invoker.UndoLast();
    public bool Redo() => _invoker.Redo();

    public IEnumerable<BankAccount> GetAllAccounts() => _accounts.GetAll();
}
