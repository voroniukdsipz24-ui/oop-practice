# Відповідність завданням 1–7

Документ показує, де у проєкті реалізовано кожну вимогу з технічного завдання.

---

## Завдання 1 — Аналіз задачі та формування вимог

**Предметна область:** емуляція роботи банківської системи — рахунки клієнтів, операції внесення/зняття/переказу, нарахування відсотків, депозити.

**Актори:** клієнт банку (користувач системи).

**Функціональні вимоги:**
- Відкриття рахунків двох типів (поточний / ощадний)
- Поповнення, зняття, переказ між рахунками
- Нарахування відсотків за різними алгоритмами
- Скасування (Undo) та повторення (Redo) операцій
- Сповіщення про значущі операції
- Збереження стану у JSON / XML

**Нефункціональні вимоги:**
- Цілісність даних через інкапсуляцію та інваріанти
- Розширюваність через SOLID та патерни
- Тестованість (xUnit + Moq)
- Сумісність із .NET 9+

---

## Завдання 2 — UML-діаграма класів

Окрема SVG-діаграма (надана раніше у чаті). Покриває:
- Domain: `BankAccount` (abstract) → `CheckingAccount`, `SavingsAccount`; `Transaction`, `Deposit`
- Strategy: `IInterestStrategy` → `Simple`/`Compound`/`Tiered`
- Command: `ICommand` → `Deposit`/`Withdraw`/`Transfer` + `CommandInvoker`
- Observer: `IAccountObserver` → `ConsoleLog`/`Email`/`Sms`
- Generics: `IRepository<T>` → `InMemoryRepository<T>` → `AccountRepository`

---

## Завдання 3 — Інкапсуляція, конструктори, властивості

| Вимога | Файл |
|---|---|
| Приватні поля | `Domain/Entities/BankAccount.cs` (`_balance`, `_observers`, `_transactions`) |
| Властивості з валідацією | `Balance` (set перевіряє мінімум), `OverdraftLimit`, `Rate` |
| Перевантажені конструктори | `CheckingAccount(owner, init)` і `CheckingAccount(owner, init, overdraft)` |
| `ToString`/`Equals`/`GetHashCode` | усі сутності |
| Перевантаження операторів | `==`, `!=` у `BankAccount` |
| .NET 9 проєкт | усі `.csproj` мають `<TargetFramework>net9.0</TargetFramework>` |

---

## Завдання 4 — Наслідування та поліморфізм

| Вимога | Файл |
|---|---|
| Абстрактний клас | `BankAccount` (`abstract`, `protected abstract GetMinAllowedBalance`) |
| Наслідування | `CheckingAccount`, `SavingsAccount` |
| `virtual`/`override` | `Deposit`, `Withdraw` (virtual у базі, поведінка специфічна у похідних) |
| Інтерфейси (ISP) | `IInterestStrategy`, `ICommand`, `IAccountObserver`, `IRepository<T>` — всі малі та сфокусовані |
| Поліморфізм через колекцію | `IRepository<BankAccount>` повертає базовий тип, реальний об'єкт визначається у runtime |

---

## Завдання 5 — Generics, делегати, LINQ

| Вимога | Файл |
|---|---|
| Узагальнений клас | `Infrastructure/Repositories/Repositories.cs` — `InMemoryRepository<T> where T : class` |
| Делегати (`Action`, `Func`) | `Application/Common/Algorithms.cs` — `ForEach`, `Map`, `Reduce` |
| Власний делегат | `TransactionFilter` |
| Лямбда-вирази | у `Program.cs` (`a => a.Balance`, `(acc, x) => acc + x`) |
| LINQ: фільтрація | `repository.Wealthy(1000m)` → `Where` |
| LINQ: проекція | `Select(a => a.Owner)` |
| LINQ: агрегація | `TotalBalance` → `Sum` |
| LINQ: групування | `GroupByOwner` → `GroupBy` |
| Extension methods | `AccountExtensions` (`TotalBalance`, `Wealthy`, `GroupByOwner`, `RecentTransactions`) |

---

## Завдання 6 — SOLID, патерни, винятки

### Принципи SOLID
| Принцип | Реалізація |
|---|---|
| **S**RP | `BankAccount` зберігає стан, `BankingService` координує, `AccountSerializer` — серіалізація, нотифікатори — сповіщення. Кожен клас має одну відповідальність. |
| **O**CP | Нові стратегії/команди/обсервери додаються без модифікації існуючого коду (через інтерфейси) |
| **L**SP | `CheckingAccount` та `SavingsAccount` повністю замінюють `BankAccount` без порушення контракту |
| **I**SP | Маленькі сфокусовані інтерфейси (`ICommand` має 2 методи, `IInterestStrategy` — 1) |
| **D**IP | `BankingService` залежить від `IRepository<BankAccount>`, не від `AccountRepository` |

### Патерни проєктування
| Патерн | Категорія | Файл |
|---|---|---|
| **Strategy** | Поведінковий | `Application/Strategies/InterestStrategies.cs` |
| **Command** | Поведінковий | `Application/Commands/Commands.cs` |
| **Observer** | Поведінковий | `Application/Observers/Observers.cs` + події у `BankAccount` |
| **Factory Method** | Породжувальний | `Application/Factories/Factories.cs` (`AccountFactory`) |
| **Singleton** | Породжувальний | `Application/Factories/Factories.cs` (`AuditLogger`) |
| **Facade** | Структурний | `Application/Services/BankingService.cs` |

### Обробка винятків
| Вимога | Файл |
|---|---|
| Власні типи винятків | `Domain/Exceptions/Exceptions.cs` — `BankingException`, `InsufficientFundsException`, `AccountNotFoundException`, `DepositNotMaturedException`, `InvalidTransactionException` |
| `try/catch` | `Program.cs` (демо retry), `RetryPolicy.cs` |
| Retry policy з експоненційною затримкою | `Application/Common/Algorithms.cs` — `RetryPolicy.Execute()` |

---

## Завдання 7 — Тести, серіалізація, документація

| Вимога | Файл |
|---|---|
| JSON-серіалізація | `Infrastructure/Serialization/AccountSerializer.cs` (System.Text.Json) |
| XML-серіалізація | `Infrastructure/Serialization/XmlAccountSerializer.cs` (System.Xml.Serialization) |
| DTO для уникнення проблем вкладеності | `AccountDto` |
| xUnit-тести | `BankSystem.Tests/UnitTests.cs` + `AdditionalTests.cs` (~30 тестів) |
| AAA-структура | усі тести розділені на Arrange/Act/Assert |
| Параметризовані тести | `[Theory]` + `[InlineData]` (Deposit, Factory) |
| Moq для ізоляції залежностей | тести `ObserverTests` (мок `IAccountObserver`) |
| Документація | `README.md` (українською) + цей файл |

---

## Підсумок покриття

| # | Завдання | Статус |
|---|---|---|
| 1 | Аналіз задачі та вимоги | ✅ (цей документ + README) |
| 2 | UML-діаграма | ✅ (SVG у чаті) |
| 3 | Інкапсуляція, конструктори, властивості | ✅ |
| 4 | Наслідування, поліморфізм, інтерфейси | ✅ |
| 5 | Generics, делегати, LINQ, extensions | ✅ |
| 6 | SOLID, патерни (5 шт.), винятки, retry | ✅ |
| 7 | xUnit + Moq, JSON + XML, документація | ✅ |
