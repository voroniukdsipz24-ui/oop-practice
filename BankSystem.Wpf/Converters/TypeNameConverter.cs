using System.Globalization;
using System.Windows.Data;
using BankSystem.Domain.Strategies;
using BankSystem.Domain.Entities;

namespace BankSystem.Wpf.Converters;

public sealed class TypeNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            CheckingAccount c => $"Поточний (овердрафт: {c.OverdraftLimit:C})",
            SavingsAccount s => $"Ощадний ({StrategyName(s.InterestStrategy.GetType())})",
            BankAccount _ => "Рахунок",
            _ => ""
        };
    }

    private static string StrategyName(Type t) => t.Name switch
    {
        nameof(SimpleInterestStrategy) => "простий %",
        nameof(CompoundInterestStrategy) => "складний %",
        nameof(TieredInterestStrategy) => "диференційований %",
        _ => t.Name
    };

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
