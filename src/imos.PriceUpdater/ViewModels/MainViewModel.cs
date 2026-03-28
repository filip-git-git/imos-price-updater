using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;
using Microsoft.Win32;

namespace IMOS.PriceUpdater.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ICsvParser _csvParser;
    private readonly PriceUpdateService _priceUpdateService;
    private string? _csvFilePath;
    private string? _configFilePath;
    private bool _isExecuting;
    private int _progressValue;
    private string _statusMessage = "Ready";
    private string _resultSummary = string.Empty;
    private PriceUpdateConfiguration? _configuration;
    private int _totalRows;
    private int _updatedCount;
    private int _skippedCount;
    private int _errorCount;
    private double _durationSeconds;
    private double _successRate;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public MainViewModel() : this(new CsvParser(), new PriceUpdateService(new CsvParser(), null))
    {
    }

    public MainViewModel(ICsvParser csvParser, PriceUpdateService priceUpdateService)
    {
        _csvParser = csvParser;
        _priceUpdateService = priceUpdateService;
        SelectCsvFileCommand = new RelayCommand(SelectCsvFile);
        SelectConfigFileCommand = new RelayCommand(SelectConfigFile);
        ExecuteCommand = new RelayCommand(Execute, CanExecute);
    }

    public string? CsvFilePath
    {
        get => _csvFilePath;
        set
        {
            if (SetProperty(ref _csvFilePath, value))
            {
                OnPropertyChanged(nameof(HasCsvFile));
            }
        }
    }

    public string? ConfigFilePath
    {
        get => _configFilePath;
        set
        {
            if (SetProperty(ref _configFilePath, value))
            {
                OnPropertyChanged(nameof(HasConfigFile));
            }
        }
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        set
        {
            if (SetProperty(ref _isExecuting, value))
            {
                OnPropertyChanged(nameof(IsNotExecuting));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsNotExecuting => !IsExecuting;

    public int ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string ResultSummary
    {
        get => _resultSummary;
        set => SetProperty(ref _resultSummary, value);
    }

    public int TotalRows
    {
        get => _totalRows;
        set => SetProperty(ref _totalRows, value);
    }

    public int UpdatedCount
    {
        get => _updatedCount;
        set => SetProperty(ref _updatedCount, value);
    }

    public int SkippedCount
    {
        get => _skippedCount;
        set => SetProperty(ref _skippedCount, value);
    }

    public int ErrorCount
    {
        get => _errorCount;
        set => SetProperty(ref _errorCount, value);
    }

    public double DurationSeconds
    {
        get => _durationSeconds;
        set => SetProperty(ref _durationSeconds, value);
    }

    public double SuccessRate
    {
        get => _successRate;
        set => SetProperty(ref _successRate, value);
    }

    public bool HasCsvFile => !string.IsNullOrEmpty(CsvFilePath);
    public bool HasConfigFile => !string.IsNullOrEmpty(ConfigFilePath);

    public ICommand SelectCsvFileCommand { get; }
    public ICommand SelectConfigFileCommand { get; }
    public ICommand ExecuteCommand { get; }

    private void SelectCsvFile(object? parameter)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select CSV File",
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() == true)
        {
            CsvFilePath = dialog.FileName;
            StatusMessage = $"CSV file selected: {Path.GetFileName(CsvFilePath)}";
        }
    }

    private void SelectConfigFile(object? parameter)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Configuration File",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            ConfigFilePath = dialog.FileName;
            StatusMessage = $"Configuration file selected: {Path.GetFileName(ConfigFilePath)}";
            _ = LoadConfigurationAsync();
        }
    }

    private async Task LoadConfigurationAsync()
    {
        if (string.IsNullOrEmpty(ConfigFilePath) || !File.Exists(ConfigFilePath))
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(ConfigFilePath);
            _configuration = JsonSerializer.Deserialize<PriceUpdateConfiguration>(json, JsonOptions);
        }
        catch
        {
            _configuration = null;
        }
    }

    private bool CanExecute(object? parameter)
    {
        return IsNotExecuting && HasCsvFile && HasConfigFile && _configuration != null;
    }

    private async void Execute(object? parameter)
    {
        if (string.IsNullOrEmpty(CsvFilePath) || _configuration == null)
        {
            ResultSummary = "Please select CSV file and configuration";
            return;
        }

        IsExecuting = true;
        ProgressValue = 0;
        ResultSummary = string.Empty;
        StatusMessage = "Processing...";

        try
        {
            var progress = new Progress<ExecutionProgress>(p =>
            {
                ProgressValue = p.Percentage;
                StatusMessage = $"Processing row {p.CurrentRow} of {p.TotalRows}...";
            });

            var summary = await _priceUpdateService.ExecuteUpdateAsync(
                _configuration,
                CsvFilePath,
                progress);

            ProgressValue = 100;
            StatusMessage = "Processing complete";

            TotalRows = summary.TotalRows;
            UpdatedCount = summary.UpdatedCount;
            SkippedCount = summary.SkippedCount;
            ErrorCount = summary.ErrorCount;
            DurationSeconds = summary.DurationSeconds;
            SuccessRate = summary.SuccessRate;

            ResultSummary = $"Total rows: {summary.TotalRows}\n" +
                          $"Updated: {summary.UpdatedCount}\n" +
                          $"Skipped (not found): {summary.SkippedCount}\n" +
                          $"Errors: {summary.ErrorCount}\n" +
                          $"Duration: {summary.DurationSeconds:F2}s\n" +
                          $"Success rate: {summary.SuccessRate:F1}%";

            var updatedItems = summary.Results.Where(r => r.Status == UpdateStatus.Success).Take(10).ToList();
            if (updatedItems.Count > 0)
            {
                ResultSummary += "\n\n--- UPDATED ---";
                foreach (var item in updatedItems)
                {
                    ResultSummary += $"\nRow {item.CsvLineNumber}: {item.SearchValue}";
                }
                if (updatedItems.Count == 10)
                {
                    ResultSummary += "\n... (more)";
                }
            }

            var skippedItems = summary.Results.Where(r => r.Status == UpdateStatus.Skipped).Take(10).ToList();
            if (skippedItems.Count > 0)
            {
                ResultSummary += "\n\n--- NOT FOUND IN DATABASE ---";
                foreach (var item in skippedItems)
                {
                    ResultSummary += $"\nRow {item.CsvLineNumber}: {item.SearchValue}";
                }
                if (skippedItems.Count == 10)
                {
                    ResultSummary += "\n... (more)";
                }
            }

            var errorItems = summary.Results.Where(r => r.Status == UpdateStatus.Error).Take(10).ToList();
            if (errorItems.Count > 0)
            {
                ResultSummary += "\n\n--- ERRORS ---";
                foreach (var item in errorItems)
                {
                    ResultSummary += $"\nRow {item.CsvLineNumber}: {item.SearchValue} - {item.ErrorMessage}";
                }
                if (errorItems.Count == 10)
                {
                    ResultSummary += "\n... (more)";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            ResultSummary = $"Failed to process CSV file: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
        }
    }
}

