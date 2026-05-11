using System.Xml.Serialization;
using BankSystem.Domain.Strategies;
using BankSystem.Domain.Entities;
using BankSystem.Domain.Interfaces;

namespace BankSystem.Infrastructure.Serialization;

/// <summary>XML serialization (Task 7 — JSON/XML support).</summary>
public static class XmlAccountSerializer
{
    public static void SaveToFile(IEnumerable<BankAccount> accounts, string path)
    {
        var dtos = accounts.Select(ToDto).ToList();
        var serializer = new XmlSerializer(typeof(List<AccountDto>));
        using var writer = new StreamWriter(path);
        serializer.Serialize(writer, dtos);
    }

    public static IEnumerable<BankAccount> LoadFromFile(string path)
    {
        var serializer = new XmlSerializer(typeof(List<AccountDto>));
        using var reader = new StreamReader(path);
        var dtos = (List<AccountDto>?)serializer.Deserialize(reader) ?? new List<AccountDto>();
        return dtos.Select(FromDto);
    }

    private static AccountDto ToDto(BankAccount acc) => acc switch
    {
        CheckingAccount c => new AccountDto
        {
            Id = c.Id, Owner = c.Owner, Balance = c.Balance,
            AccountType = "Checking", OverdraftLimit = c.OverdraftLimit
        },
        SavingsAccount s => new AccountDto
        {
            Id = s.Id, Owner = s.Owner, Balance = s.Balance,
            AccountType = "Savings",
            StrategyType = s.InterestStrategy.GetType().Name,
            InterestRate = 0.05m
        },
        _ => throw new NotSupportedException($"Unknown account type: {acc.GetType()}")
    };

    private static BankAccount FromDto(AccountDto dto) => dto.AccountType switch
    {
        "Checking" => new CheckingAccount(dto.Owner, dto.Balance, dto.OverdraftLimit ?? 0),
        "Savings" => new SavingsAccount(
            dto.Owner, dto.Balance,
            CreateStrategy(dto.StrategyType, dto.InterestRate ?? 0.05m)),
        _ => throw new NotSupportedException($"Unknown account type: {dto.AccountType}")
    };

    private static IInterestStrategy CreateStrategy(string? type, decimal rate) => type switch
    {
        nameof(CompoundInterestStrategy) => new CompoundInterestStrategy(rate),
        _ => new SimpleInterestStrategy(rate)
    };
}
