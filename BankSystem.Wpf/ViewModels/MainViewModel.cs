using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using BankSystem.Application.Commands;
using BankSystem.Application.Factories;
using BankSystem.Application.Services;
using BankSystem.Domain.Entities;
using BankSystem.Infrastructure.Repositories;
using BankSystem.Infrastructure.Serialization;
using Microsoft.Win32;

namespace BankSystem.Wpf.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly AccountRepository _repository;
    private readonly CommandInvoker _invoker;
    private readonly BankingService _bankingService;
    private readonly UiNotificationObserver _uiObserver;

    public ObservableCollection<BankAccount> Accounts { get; } = new();
    public ObservableCollection<string> Notifications => _uiObserver.Messages;

    private BankAccount? _selectedAccount;
    public BankAccount? SelectedAccount
    {
        get => _selectedAccount;
        set { SetField(ref _selectedAccount, value); RefreshTransactions(); }
    }

    private BankAccount? _transferTargetAccount;
    public BankAccount? TransferTargetAccount
    {
        get => _transferTargetAccount;
        set => SetField(ref _transferTargetAccount, value);
    }

    public ObservableCollection<Transaction> SelectedTransactions { get; } = new();

    private string _newOwner = "";
    public string NewOwner { get => _newOwner; set => SetField(ref _newOwner, value); }

    private decimal _newInitialBalance = 1000m;
    public decimal NewInitialBalance { get => _newInitialBalance; set => SetField(ref _newInitialBalance, value); }

    private decimal _newOverdraft;
    public decimal NewOverdraft { get => _newOverdraft; set => SetField(ref _newOverdraft, value); }

    private string _selectedAccountType = "Поточний";
    public string SelectedAccountType
    {
        get => _selectedAccountType;
        set => SetField(ref _selectedAccountType, value);
    }
    public string[] AccountTypes { get; } =
    {
        "Поточний",
        "Ощадний (простий %)",
        "Ощадний (складний %)",
        "Ощадний (диференційований %)"
    };

    private decimal _transactionAmount = 100m;
    public decimal TransactionAmount { get => _transactionAmount; set => SetField(ref _transactionAmount, value); }

    private decimal _interestRate = 0.05m;
    public decimal InterestRate { get => _interestRate; set => SetField(ref _interestRate, value); }

    private string _statusMessage = "Готово. Завантажено демо-дані — спробуйте операції →";
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    public decimal TotalBalance => Accounts.Sum(a => a.Balance);

    public ICommand OpenAccountCommand { get; }
    public ICommand DepositCommand { get; }
    public ICommand WithdrawCommand { get; }
    public ICommand TransferCommand { get; }
    public ICommand ApplyInterestCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand SaveJsonCommand { get; }
    public ICommand SaveXmlCommand { get; }
    public ICommand LoadJsonCommand { get; }

    public MainViewModel()
    {
        _repository = new AccountRepository();
        _invoker = new CommandInvoker();
        _bankingService = new BankingService(_repository, _invoker);
        _uiObserver = new UiNotificationObserver();

        OpenAccountCommand = new RelayCommand(OpenAccount, () => !string.IsNullOrWhiteSpace(NewOwner));
        DepositCommand = new RelayCommand(Deposit, () => SelectedAccount != null && TransactionAmount > 0);
        WithdrawCommand = new RelayCommand(Withdraw, () => SelectedAccount != null && TransactionAmount > 0);
        TransferCommand = new RelayCommand(Transfer,
            () => SelectedAccount != null && TransferTargetAccount != null
                  && SelectedAccount != TransferTargetAccount && TransactionAmount > 0);
        ApplyInterestCommand = new RelayCommand(ApplyInterest, () => SelectedAccount is SavingsAccount);
        UndoCommand = new RelayCommand(Undo);
        RedoCommand = new RelayCommand(Redo);
        SaveJsonCommand = new RelayCommand(SaveJson, () => Accounts.Count > 0);
        SaveXmlCommand = new RelayCommand(SaveXml, () => Accounts.Count > 0);
        LoadJsonCommand = new RelayCommand(LoadFromFile);

        SeedSampleData();
    }

    private void SeedSampleData()
    {
        var alice = AccountFactory.CreateChecking("Аліса", 1500m, 500m);
        var bob = AccountFactory.CreateSavings("Богдан", 5000m, "compound", 0.05m);
        AddAccount(alice);
        AddAccount(bob);
    }

    private void AddAccount(BankAccount account)
    {
        _repository.Add(account);
        account.Subscribe(_uiObserver);
        Accounts.Add(account);
        OnPropertyChanged(nameof(TotalBalance));
    }

    private void OpenAccount()
    {
        try
        {
            BankAccount account = SelectedAccountType switch
            {
                "Поточний" => AccountFactory.CreateChecking(NewOwner, NewInitialBalance, NewOverdraft),
                "Ощадний (простий %)" => AccountFactory.CreateSavings(NewOwner, NewInitialBalance, "simple", InterestRate),
                "Ощадний (складний %)" => AccountFactory.CreateSavings(NewOwner, NewInitialBalance, "compound", InterestRate),
                "Ощадний (диференційований %)" => AccountFactory.CreateSavings(NewOwner, NewInitialBalance, "tiered", InterestRate),
                _ => throw new InvalidOperationException("Невідомий тип рахунку")
            };
            AddAccount(account);
            SetStatus($"Відкрито рахунок «{SelectedAccountType}» для {NewOwner}");
            NewOwner = "";
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Deposit()
    {
        try
        {
            var owner = SelectedAccount!.Owner;
            _bankingService.Deposit(SelectedAccount.Id, TransactionAmount);
            var amount = TransactionAmount;
            RefreshAfterTransaction();
            SetStatus($"Поповнено {amount:C} на рахунок {owner}");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Withdraw()
    {
        try
        {
            var owner = SelectedAccount!.Owner;
            _bankingService.Withdraw(SelectedAccount.Id, TransactionAmount);
            var amount = TransactionAmount;
            RefreshAfterTransaction();
            SetStatus($"Знято {amount:C} з рахунку {owner}");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Transfer()
    {
        try
        {
            var fromOwner = SelectedAccount!.Owner;
            var toOwner = TransferTargetAccount!.Owner;
            var amount = TransactionAmount;
            _bankingService.Transfer(SelectedAccount.Id, TransferTargetAccount.Id, amount);
            RefreshAfterTransaction();
            SetStatus($"Переказано {amount:C}: {fromOwner} → {toOwner}");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ApplyInterest()
    {
        try
        {
            var account = (SavingsAccount)SelectedAccount!;
            var owner = account.Owner;
            account.ApplyInterest();
            RefreshAfterTransaction();
            SetStatus($"Нараховано відсотки на рахунок {owner}");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void Undo()
    {
        if (_bankingService.UndoLast())
        {
            RefreshAfterTransaction();
            SetStatus("Останню операцію скасовано");
        }
        else SetStatus("Немає операцій для скасування");
    }

    private void Redo()
    {
        if (_bankingService.Redo())
        {
            RefreshAfterTransaction();
            SetStatus("Операцію повторено");
        }
        else SetStatus("Немає операцій для повторення");
    }

    // ===== File dialogs =====
    private void SaveJson()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Зберегти стан системи у JSON",
            Filter = "JSON-файли (*.json)|*.json|Усі файли (*.*)|*.*",
            FileName = $"bank-state-{DateTime.Now:yyyy-MM-dd-HHmm}.json",
            DefaultExt = ".json",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() != true) { SetStatus("Збереження скасовано"); return; }

        try
        {
            AccountSerializer.SaveToFile(_repository.GetAll(), dialog.FileName);
            SetStatus($"Збережено JSON: {dialog.FileName}");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void SaveXml()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Зберегти стан системи у XML",
            Filter = "XML-файли (*.xml)|*.xml|Усі файли (*.*)|*.*",
            FileName = $"bank-state-{DateTime.Now:yyyy-MM-dd-HHmm}.xml",
            DefaultExt = ".xml",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() != true) { SetStatus("Збереження скасовано"); return; }

        try
        {
            XmlAccountSerializer.SaveToFile(_repository.GetAll(), dialog.FileName);
            SetStatus($"Збережено XML: {dialog.FileName}");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void LoadFromFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Завантажити стан системи",
            Filter = "Усі підтримувані (*.json;*.xml)|*.json;*.xml|JSON-файли (*.json)|*.json|XML-файли (*.xml)|*.xml",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() != true) { SetStatus("Завантаження скасовано"); return; }

        try
        {
            var path = dialog.FileName;
            var extension = Path.GetExtension(path).ToLowerInvariant();

            // Ask before replacing current data
            if (Accounts.Count > 0)
            {
                var result = MessageBox.Show(
                    "У системі вже є рахунки. Замінити їх даними з файлу?",
                    "Підтвердження",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel) { SetStatus("Завантаження скасовано"); return; }
                if (result == MessageBoxResult.Yes) ClearAll();
            }

            IEnumerable<BankAccount> loaded = extension switch
            {
                ".json" => AccountSerializer.LoadFromFile(path),
                ".xml" => XmlAccountSerializer.LoadFromFile(path),
                _ => throw new NotSupportedException("Підтримуються лише файли .json та .xml")
            };

            int count = 0;
            foreach (var acc in loaded)
            {
                if (!_repository.Exists(acc.Id)) { AddAccount(acc); count++; }
            }
            SetStatus($"Завантажено {count} рахунків з {Path.GetFileName(path)}");
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private void ClearAll()
    {
        foreach (var acc in Accounts.ToList())
            _repository.Remove(acc.Id);
        Accounts.Clear();
        SelectedTransactions.Clear();
    }

    private void RefreshAfterTransaction()
    {
        // Зберігаємо id вибраного рахунку перед оновленням
        var selectedId = SelectedAccount?.Id;
        var snapshot = Accounts.ToList();
        Accounts.Clear();
        foreach (var a in snapshot) Accounts.Add(a);

        // Відновлюємо вибраний рахунок
        if (selectedId.HasValue)
            SelectedAccount = Accounts.FirstOrDefault(a => a.Id == selectedId.Value);

        RefreshTransactions();
        OnPropertyChanged(nameof(TotalBalance));
    }

    private void RefreshTransactions()
    {
        SelectedTransactions.Clear();
        if (SelectedAccount is null) return;
        foreach (var t in SelectedAccount.Transactions.Reverse())
            SelectedTransactions.Add(t);
    }

    private void SetStatus(string msg) => StatusMessage = msg;

    private void ShowError(Exception ex)
    {
        StatusMessage = $"Помилка: {ex.Message}";
        MessageBox.Show(ex.Message, "Не вдалося виконати операцію",
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
