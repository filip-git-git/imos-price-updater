using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;
using Microsoft.Win32;

namespace IMOS.PriceUpdater.ViewModels;

/// <summary>
///     ViewModel for the price update execution view.
/// </summary>
public sealed class PriceUpdateViewModel : ViewModelBase
{
    private readonly IPriceUpdateService _priceUpdateService;
    private readonly ICsvParser _csvParser;
    private string _csvFilePath = string.Empty;
    private string _configFilePath = string.Empty;
    private int _totalRows;
    private int _processedRows;
    private int _updatedCount;
    private int _skippedCount;
    private int _errorCount;
    private bool _isExecuting;
    private bool _hasStarted;
    private bool _isComplete;
    private string _statusMessage = "Ready";
    private string _errorMessage = string.Empty;
    private double _progressPercentage;
    private PriceUpdateConfiguration? _configuration;
    private ExecutionSummary? _lastExecutionSummary;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    ///     Initializes a new instance of the PriceUpdateViewModel class.
    /// </summary>
    public PriceUpdateViewModel() : this(new PriceUpdateService(new CsvParser(), null!), new CsvParser())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the PriceUpdateViewModel class with injected services.
    /// </summary>
    /// <param name="priceUpdateService">The price update service.</param>
    /// <param name="csvParser">The CSV parser service.</param>
    public PriceUpdateViewModel(IPriceUpdateService priceUpdateService, ICsvParser csvParser)
    {
        _priceUpdateService = priceUpdateService ?? throw new ArgumentNullException(nameof(priceUpdateService));
        _csvParser = csvParser ?? throw new ArgumentNullException(nameof(csvParser));

        ExecuteCommand = new RelayCommand(
            ExecuteExecuteCommand,
            CanExecuteExecuteCommand);

        CancelCommand = new RelayCommand(
            ExecuteCancelCommand,
            CanExecuteCancelCommand);

        SelectCsvFileCommand = new RelayCommand(_ => SelectCsvFile());
        LoadConfigurationCommand = new RelayCommand(_ => LoadConfiguration(), _ => CanLoadConfiguration);
        ClearCommand = new RelayCommand(_ => ClearAll());

        Results = new ObservableCollection<UpdateResult>();
    }

    #region Properties

    /// <summary>
    ///     Gets or sets the selected CSV file path.
    /// </summary>
    public string CsvFilePath
    {
        get => _csvFilePath;
        set
        {
            if (SetProperty(ref _csvFilePath, value))
            {
                OnPropertyChanged(nameof(HasCsvFile));
                OnPropertyChanged(nameof(CanLoadConfiguration));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the selected configuration file path.
    /// </summary>
    public string ConfigFilePath
    {
        get => _configFilePath;
        set
        {
            if (SetProperty(ref _configFilePath, value))
            {
                OnPropertyChanged(nameof(HasConfigFile));
                OnPropertyChanged(nameof(CanLoadConfiguration));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the total number of rows in the CSV file.
    /// </summary>
    public int TotalRows
    {
        get => _totalRows;
        private set => SetProperty(ref _totalRows, value);
    }

    /// <summary>
    ///     Gets or sets the number of processed rows.
    /// </summary>
    public int ProcessedRows
    {
        get => _processedRows;
        private set
        {
            if (SetProperty(ref _processedRows, value))
            {
                ProgressPercentage = TotalRows > 0 ? (double)value / TotalRows * 100 : 0;
            }
        }
    }

    /// <summary>
    ///     Gets or sets the number of successfully updated rows.
    /// </summary>
    public int UpdatedCount
    {
        get => _updatedCount;
        private set => SetProperty(ref _updatedCount, value);
    }

    /// <summary>
    ///     Gets or sets the number of skipped rows.
    /// </summary>
    public int SkippedCount
    {
        get => _skippedCount;
        private set => SetProperty(ref _skippedCount, value);
    }

    /// <summary>
    ///     Gets or sets the number of rows with errors.
    /// </summary>
    public int ErrorCount
    {
        get => _errorCount;
        private set => SetProperty(ref _errorCount, value);
    }

    /// <summary>
    ///     Gets or sets whether an update is in progress.
    /// </summary>
    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (SetProperty(ref _isExecuting, value))
            {
                OnPropertyChanged(nameof(IsNotExecuting));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    ///     Gets a value indicating whether an update is not in progress.
    /// </summary>
    public bool IsNotExecuting => !IsExecuting;

    /// <summary>
    ///     Gets or sets whether execution has started.
    /// </summary>
    public bool HasStarted
    {
        get => _hasStarted;
        private set => SetProperty(ref _hasStarted, value);
    }

    /// <summary>
    ///     Gets or sets whether execution has completed.
    /// </summary>
    public bool IsComplete
    {
        get => _isComplete;
        private set => SetProperty(ref _isComplete, value);
    }

    /// <summary>
    ///     Gets or sets the current status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    ///     Gets or sets the error message.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    ///     Gets a value indicating whether there is an error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    ///     Gets or sets the progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage
    {
        get => _progressPercentage;
        private set => SetProperty(ref _progressPercentage, value);
    }

    /// <summary>
    ///     Gets or sets the current configuration.
    /// </summary>
    public PriceUpdateConfiguration? Configuration
    {
        get => _configuration;
        private set
        {
            if (SetProperty(ref _configuration, value))
            {
                OnPropertyChanged(nameof(HasConfiguration));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the last execution summary.
    /// </summary>
    public ExecutionSummary? LastExecutionSummary
    {
        get => _lastExecutionSummary;
        private set => SetProperty(ref _lastExecutionSummary, value);
    }

    /// <summary>
    ///     Gets the collection of update results.
    /// </summary>
    public ObservableCollection<UpdateResult> Results { get; }

    /// <summary>
    ///     Gets a value indicating whether a CSV file is selected.
    /// </summary>
    public bool HasCsvFile => !string.IsNullOrWhiteSpace(CsvFilePath);

    /// <summary>
    ///     Gets a value indicating whether a configuration file is selected.
    /// </summary>
    public bool HasConfigFile => !string.IsNullOrWhiteSpace(ConfigFilePath);

    /// <summary>
    ///     Gets a value indicating whether a configuration is loaded.
    /// </summary>
    public bool HasConfiguration => Configuration != null;

    /// <summary>
    ///     Gets a value indicating whether the configuration can be loaded.
    /// </summary>
    public bool CanLoadConfiguration => HasCsvFile && HasConfigFile;

    #endregion

    #region Commands

    /// <summary>
    ///     Gets the execute command.
    /// </summary>
    public ICommand ExecuteCommand { get; }

    /// <summary>
    ///     Gets the cancel command.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    ///     Gets the select CSV file command.
    /// </summary>
    public ICommand SelectCsvFileCommand { get; }

    /// <summary>
    ///     Gets the load configuration command.
    /// </summary>
    public ICommand LoadConfigurationCommand { get; }

    /// <summary>
    ///     Gets the clear command.
    /// </summary>
    public ICommand ClearCommand { get; }

    #endregion

    #region Command Implementations

    private bool CanExecuteExecuteCommand(object? parameter)
    {
        return !IsExecuting && HasCsvFile && HasConfiguration;
    }

    private async void ExecuteExecuteCommand(object? parameter)
    {
        await ExecuteUpdateAsync();
    }

    private bool CanExecuteCancelCommand(object? parameter)
    {
        return IsExecuting;
    }

    private void ExecuteCancelCommand(object? parameter)
    {
        CancelExecution();
    }

    #endregion

    #region Public Methods

    /// <summary>
    ///     Selects a CSV file via file dialog.
    /// </summary>
    public void SelectCsvFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Title = "Select CSV File"
        };

        if (dialog.ShowDialog() == true)
        {
            CsvFilePath = dialog.FileName;
            StatusMessage = $"Selected: {Path.GetFileName(CsvFilePath)}";
            _ = LoadCsvInfoAsync();
        }
    }

    /// <summary>
    ///     Loads a configuration file via file dialog.
    /// </summary>
    public void LoadConfiguration()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Select Configuration File"
        };

        if (dialog.ShowDialog() == true)
        {
            ConfigFilePath = dialog.FileName;
            LoadConfigurationFromFile(ConfigFilePath);
        }
    }

    /// <summary>
    ///     Clears all state and results.
    /// </summary>
    public void ClearAll()
    {
        CsvFilePath = string.Empty;
        ConfigFilePath = string.Empty;
        Configuration = null;
        TotalRows = 0;
        ProcessedRows = 0;
        UpdatedCount = 0;
        SkippedCount = 0;
        ErrorCount = 0;
        ProgressPercentage = 0;
        StatusMessage = "Ready";
        ErrorMessage = string.Empty;
        HasStarted = false;
        IsComplete = false;
        LastExecutionSummary = null;
        Results.Clear();
    }

    /// <summary>
    ///     Cancels the current execution.
    /// </summary>
    public void CancelExecution()
    {
        if (IsExecuting && _cancellationTokenSource != null)
        {
            StatusMessage = "Cancelling...";
            _cancellationTokenSource.Cancel();
        }
    }

    #endregion

    #region Private Methods

    private async Task LoadCsvInfoAsync()
    {
        if (!HasCsvFile)
        {
            return;
        }

        try
        {
            TotalRows = await _priceUpdateService.CountCsvRowsAsync(CsvFilePath);
            StatusMessage = $"Loaded {TotalRows} rows from {Path.GetFileName(CsvFilePath)}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load CSV: {ex.Message}";
            TotalRows = 0;
        }
    }

    private void LoadConfigurationFromFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            Configuration = System.Text.Json.JsonSerializer.Deserialize<PriceUpdateConfiguration>(json);
            StatusMessage = $"Loaded configuration from {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load configuration: {ex.Message}";
            Configuration = null;
        }
    }

    private async Task ExecuteUpdateAsync()
    {
        if (string.IsNullOrWhiteSpace(CsvFilePath) || Configuration == null)
        {
            ErrorMessage = "Please select a CSV file and configuration.";
            return;
        }

        // Validate configuration
        var validationResult = _priceUpdateService.ValidateConfiguration(Configuration);
        if (!validationResult.IsValid)
        {
            ErrorMessage = $"Configuration error: {string.Join("; ", validationResult.Errors)}";
            return;
        }

        IsExecuting = true;
        HasStarted = true;
        IsComplete = false;
        ClearResults();
        ErrorMessage = string.Empty;
        StatusMessage = "Starting update...";

        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        var progress = new Progress<ExecutionProgress>(OnProgressChanged);

        try
        {
            var summary = await _priceUpdateService.ExecuteUpdateAsync(
                Configuration,
                CsvFilePath,
                progress,
                cancellationToken);

            LastExecutionSummary = summary;
            UpdatedCount = summary.UpdatedCount;
            SkippedCount = summary.SkippedCount;
            ErrorCount = summary.ErrorCount;

            if (summary.HasErrors)
            {
                StatusMessage = $"Completed with {summary.ErrorCount} errors";
            }
            else
            {
                StatusMessage = $"Successfully updated {summary.UpdatedCount} rows";
            }

            IsComplete = true;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Update cancelled by user";
            ErrorMessage = "The update operation was cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Update failed";
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsExecuting = false;
            ProcessedRows = TotalRows;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void OnProgressChanged(ExecutionProgress progress)
    {
        ProcessedRows = progress.CurrentRow;
        StatusMessage = progress.Message ?? $"Processed {progress.CurrentRow} of {progress.TotalRows} rows";
    }

    private void ClearResults()
    {
        Results.Clear();
        UpdatedCount = 0;
        SkippedCount = 0;
        ErrorCount = 0;
        ProcessedRows = 0;
        ProgressPercentage = 0;
    }

    #endregion
}

