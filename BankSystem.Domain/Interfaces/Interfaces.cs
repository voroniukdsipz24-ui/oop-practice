using BankSystem.Domain.Entities;

namespace BankSystem.Domain.Interfaces;

// Strategy Pattern — interest calculation
public interface IInterestStrategy
{
    decimal CalculateInterest(decimal balance);
    string Description { get; }
}

// Observer Pattern — account event notifications
public interface IAccountObserver
{
    void OnAccountChanged(BankAccount account, Transaction transaction);
}

// Command Pattern
public interface ICommand
{
    void Execute();
    void Undo();
    string Description { get; }
}

// Generic repository contract
public interface IRepository<T> where T : class
{
    T? GetById(Guid id);
    IEnumerable<T> GetAll();
    void Add(T entity);
    void Remove(Guid id);
    bool Exists(Guid id);
}
