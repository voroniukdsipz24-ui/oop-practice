using BankSystem.Domain.Interfaces;

namespace BankSystem.Domain.Strategies;

/// <summary>Simple (flat) interest: I = P * r * t (t = 1 year assumed)</summary>
public sealed class SimpleInterestStrategy : IInterestStrategy
{
    private readonly decimal _annualRate;

    public string Description => $"Simple interest @ {_annualRate:P2}/yr";

    public SimpleInterestStrategy(decimal annualRate)
    {
        if (annualRate is < 0 or > 1)
            throw new ArgumentException("Annual rate must be between 0 and 1.");
        _annualRate = annualRate;
    }

    public decimal CalculateInterest(decimal balance) =>
        balance * _annualRate;
}

/// <summary>Compound interest: A = P*(1 + r/n)^n − P</summary>
public sealed class CompoundInterestStrategy : IInterestStrategy
{
    private readonly decimal _annualRate;
    private readonly int _compoundsPerYear;

    public string Description =>
        $"Compound interest @ {_annualRate:P2}/yr, {_compoundsPerYear}x/yr";

    public CompoundInterestStrategy(decimal annualRate, int compoundsPerYear = 12)
    {
        if (annualRate is < 0 or > 1)
            throw new ArgumentException("Rate must be between 0 and 1.");
        if (compoundsPerYear <= 0)
            throw new ArgumentException("Compounds per year must be positive.");

        _annualRate = annualRate;
        _compoundsPerYear = compoundsPerYear;
    }

    public decimal CalculateInterest(decimal balance)
    {
        double r = (double)_annualRate / _compoundsPerYear;
        double compound = Math.Pow(1 + r, _compoundsPerYear);
        return balance * ((decimal)compound - 1m);
    }
}

/// <summary>Tiered interest — higher rate for higher balance.</summary>
public sealed class TieredInterestStrategy : IInterestStrategy
{
    private readonly IReadOnlyList<(decimal Threshold, decimal Rate)> _tiers;

    public string Description => "Tiered interest";

    public TieredInterestStrategy(IEnumerable<(decimal Threshold, decimal Rate)> tiers)
    {
        _tiers = tiers.OrderBy(t => t.Threshold).ToList();
    }

    public decimal CalculateInterest(decimal balance)
    {
        var rate = _tiers.LastOrDefault(t => balance >= t.Threshold).Rate;
        return balance * rate;
    }
}
