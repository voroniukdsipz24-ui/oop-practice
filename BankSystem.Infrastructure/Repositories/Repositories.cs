using BankSystem.Domain.Entities;
using BankSystem.Domain.Interfaces;

namespace BankSystem.Infrastructure.Repositories;

public class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly Dictionary<Guid, T> _storage = new();
    private readonly Func<T, Guid> _idSelector;

    public InMemoryRepository(Func<T, Guid> idSelector) =>
        _idSelector = idSelector ?? throw new ArgumentNullException(nameof(idSelector));

    public T? GetById(Guid id) => _storage.GetValueOrDefault(id);

    public IEnumerable<T> GetAll() => _storage.Values;

    public void Add(T entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        var id = _idSelector(entity);
        if (_storage.ContainsKey(id))
            throw new InvalidOperationException($"Entity with id {id} already exists.");
        _storage[id] = entity;
    }

    public void Remove(Guid id) => _storage.Remove(id);
    public bool Exists(Guid id) => _storage.ContainsKey(id);
}

public sealed class AccountRepository : InMemoryRepository<BankAccount>
{
    public AccountRepository() : base(a => a.Id) { }

    public IEnumerable<BankAccount> FindByOwner(string owner) =>
        GetAll().Where(a => a.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase));

    public decimal GetTotalBalance() => GetAll().Sum(a => a.Balance);
}
