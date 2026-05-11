using System.Text.Json;
using BankSystem.Domain.Strategies;
using BankSystem.Domain.Entities;
using BankSystem.Domain.Interfaces;

namespace BankSystem.Infrastructure.Serialization;

// DTO — separates persistence model from domain model
public sealed class AccountDto
{
    public Guid Id { get; set; }
    public string Owner { get; set; } = "";
    public decimal Balance { get; set; }
    public string AccountType { get; set; } = "";
    public decimal? OverdraftLimit { get; set; }
    public decimal? InterestRate { get; set; }
    public string? StrategyType { get; set; }
}

public static class AccountSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(IEnumerable<BankAccount> accounts)
    {
        var dtos = accounts.Select(ToDto).ToList();
        return JsonSerializer.Serialize(dtos, Options);
    }

    public static IEnumerable<BankAccount> Deserialize(string json)
    {
        var dtos = JsonSerializer.Deserialize<List<AccountDto>>(json, Options)
            ?? new List<AccountDto>();
        return dtos.Select(FromDto);
    }

    public static void SaveToFile(IEnumerable<BankAccount> accounts, string path) =>
        File.WriteAllText(path, Serialize(accounts));

    public static IEnumerable<BankAccount> LoadFromFile(string path) =>
        Deserialize(File.ReadAllText(path));

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
            InterestRate = 0.05m  // simplified — real version would persist strategy state
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
