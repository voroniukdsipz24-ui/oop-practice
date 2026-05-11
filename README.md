# Емулятор банківської системи

Навчальний проєкт з курсу ООП на C# / .NET 9, що демонструє застосування патернів проєктування та принципів SOLID.

## Опис

Емулятор банківської системи з підтримкою рахунків, транзакцій, депозитів, нарахування відсотків та операцій з можливістю відміни (Undo/Redo).

## Архітектура

Рішення поділене на 4 проєкти за принципом Clean Architecture:

```
BankSystem/
├── BankSystem.Domain/          # Сутності, інтерфейси, винятки
│   ├── Entities/               # BankAccount, Transaction, Deposit
│   ├── Interfaces/             # IInterestStrategy, ICommand, IAccountObserver, IRepository<T>
│   └── Exceptions/             # BankingException та похідні
├── BankSystem.Application/     # Бізнес-логіка, патерни, сервіси
│   ├── Strategies/             # Strategy: Simple/Compound/Tiered Interest
│   ├── Commands/               # Command: Deposit/Withdraw/Transfer + Invoker
│   ├── Observers/              # Observer: Console/Email/SMS notifiers
│   └── Services/               # BankingService (Facade)
├── BankSystem.Infrastructure/  # Сховища, серіалізація
│   ├── Repositories/           # InMemoryRepository<T>, AccountRepository
│   └── Serialization/          # JSON serialization з DTO
└── BankSystem.Tests/           # xUnit + Moq
```

## Застосовані патерни

### Поведінкові
- **Strategy** (`IInterestStrategy`) — підстановка алгоритму нарахування відсотків (Simple/Compound/Tiered) під час виконання без модифікації `SavingsAccount`.
- **Command** (`ICommand`) — інкапсуляція операцій (Deposit/Withdraw/Transfer) з підтримкою Undo/Redo через `CommandInvoker`.
- **Observer** (`IAccountObserver`) — підписка нотифікаторів на зміни рахунку. Підписники можуть бути відключені без memory leak.

### Породжувальні
- Конструктори з валідацією у базовому класі забезпечують ініціалізацію інваріантів.

### Структурні
- **Facade** (`BankingService`) — єдиний вхід до підсистем рахунків, команд та сповіщень.

## Принципи SOLID

| Принцип | Реалізація |
|---------|-----------|
| **S**RP | `BankAccount` зберігає стан, `BankingService` координує, `AccountSerializer` зберігає, нотифікатори сповіщають |
| **O**CP | Нові стратегії відсотків, команди та обсервери додаються без зміни існуючого коду |
| **L**SP | `CheckingAccount` та `SavingsAccount` є повноцінною заміною `BankAccount` |
| **I**SP | Малі сфокусовані інтерфейси (`ICommand`, `IInterestStrategy`, `IAccountObserver`) |
| **D**IP | `BankingService` залежить від `IRepository<BankAccount>`, не від `AccountRepository` |

## Інкапсуляція та інваріанти

- Всі поля приватні, доступ через властивості з валідацією
- Сетер `Balance` перевіряє `GetMinAllowedBalance()` (підтримка овердрафту)
- Конструктори валідують `owner`, `initialBalance`, `rate`
- Перевантажено оператори `==`, `!=` та `Equals/GetHashCode` за `Id`

## Запуск

### Вимоги
- .NET 9 SDK (або новіша версія)

### Запуск GUI (WPF) — основний варіант
```bash
cd BankSystem
dotnet build
dotnet run --project BankSystem.Wpf
```

> ⚠️ WPF працює **тільки на Windows**. Потрібен Windows 10/11 + .NET 9 SDK.

### Тести
```bash
dotnet test
```

## Приклад використання

```csharp
var repository = new AccountRepository();
var invoker = new CommandInvoker();
var bank = new BankingService(repository, invoker);

// Відкрити рахунок зі стратегією compound interest
var savings = bank.OpenSavings("Bob", 5000m, new CompoundInterestStrategy(0.05m, 12));

// Підписати спостерігача
savings.Subscribe(new ConsoleLogObserver());

// Виконати операцію (через Command pattern)
bank.Deposit(savings.Id, 1000m);

// Відмінити останню операцію
bank.UndoLast();

// Зберегти стан у JSON
AccountSerializer.SaveToFile(bank.GetAllAccounts(), "state.json");
```

## UML-діаграма

Див. діаграму класів у супровідному документі (показує Domain, Strategy, Command, Observer та Generics-сховище).

## Тестування

Реалізовано через **xUnit**. Зовнішні залежності (observer-нотифікатори) ізольовані через **Moq**.

Структура тестів — AAA (Arrange–Act–Assert):
- `BankAccountTests` — інкапсуляція, валідація, овердрафт, рівність
- `StrategyTests` — три алгоритми відсотків + підміна під час виконання
- `CommandTests` — Execute/Undo/Redo, Transfer
- `ObserverTests` — нотифікація, відписка, кілька спостерігачів (з Mock)

## Технології

- C# 13 / .NET 9
- System.Text.Json — серіалізація
- xUnit — тестування
- Moq — мокування

## Автор

Навчальний проєкт з курсу «Об'єктно-орієнтоване програмування».
