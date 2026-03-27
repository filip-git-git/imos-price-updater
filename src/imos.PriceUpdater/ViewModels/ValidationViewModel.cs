using System.Collections.ObjectModel;
using System.Windows.Input;
using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;
using Microsoft.Extensions.Logging;

namespace IMOS.PriceUpdater.ViewModels;

/// <summary>
///     ViewModel for the CSV validation dialog.
/// </summary>
public sealed class ValidationViewModel : ViewModelBase
{
    private readonly IValidationService _validationService;
    private readonly ILogger<ValidationViewModel> _logger;
    private string _csvFilePath = string.Empty;
    private bool _isValidating;
    private bool _isValid;
    private string _statusMessage = string.Empty;
    private CsvValidationResult? _validationResult;

    /// <summary>
    ///     Initializes a new instance of the ValidationViewModel class.
    /// </summary>
    /// <param name="validationService">The validation service.</param>
    /// <param name="logger">The logger instance.</param>
    public ValidationViewModel(IValidationService validationService, ILogger<ValidationViewModel> logger)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Errors = new ObservableCollection<CsvValidationError>();
        Warnings = new ObservableCollection<CsvValidationWarning>();

        ValidateCommand = new RelayCommand(async _ => await ExecuteValidateAsync(), CanExecuteValidate);
        BrowseCommand = new RelayCommand(ExecuteBrowse);
        ClearCommand = new RelayCommand(ExecuteClear);
    }

    /// <summary>
    ///     Gets or sets the CSV file path.
    /// </summary>
    public string CsvFilePath
    {
        get => _csvFilePath;
        set
        {
            if (SetProperty(ref _csvFilePath, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the validating indicator.
    /// </summary>
    public bool IsValidating
    {
        get => _isValidating;
        set => SetProperty(ref _isValidating, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the CSV is valid.
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        private set => SetProperty(ref _isValid, value);
    }

    /// <summary>
    ///     Gets or sets the status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    ///     Gets the validation result.
    /// </summary>
    public CsvValidationResult? ValidationResult
    {
        get => _validationResult;
        private set => SetProperty(ref _validationResult, value);
    }

    /// <summary>
    ///     Gets the collection of errors.
    /// </summary>
    public ObservableCollection<CsvValidationError> Errors { get; }

    /// <summary>
    ///     Gets the collection of warnings.
    /// </summary>
    public ObservableCollection<CsvValidationWarning> Warnings { get; }

    /// <summary>
    ///     Gets the total rows count.
    /// </summary>
    public int TotalRows => ValidationResult?.TotalRows ?? 0;

    /// <summary>
    ///     Gets the valid rows count.
    /// </summary>
    public int ValidRows => ValidationResult?.ValidRows ?? 0;

    /// <summary>
    ///     Gets the error count.
    /// </summary>
    public int ErrorCount => Errors.Count;

    /// <summary>
    ///     Gets the warning count.
    /// </summary>
    public int WarningCount => Warnings.Count;

    /// <summary>
    ///     Gets the validate command.
    /// </summary>
    public ICommand ValidateCommand { get; }

    /// <summary>
    ///     Gets the browse command.
    /// </summary>
    public ICommand BrowseCommand { get; }

    /// <summary>
    ///     Gets the clear command.
    /// </summary>
    public ICommand ClearCommand { get; }

    /// <summary>
    ///     Sets the configuration for validation.
    /// </summary>
    public PriceUpdateConfiguration? Configuration { get; set; }

    /// <summary>
    ///     Validates the CSV file asynchronously.
    /// </summary>
    public async Task ValidateAsync()
    {
        await ExecuteValidateAsync();
    }

    private bool CanExecuteValidate(object? parameter)
    {
        return !string.IsNullOrWhiteSpace(CsvFilePath) && !IsValidating;
    }

    private async Task ExecuteValidateAsync()
    {
        if (string.IsNullOrWhiteSpace(CsvFilePath))
        {
            StatusMessage = "Please select a CSV file";
            return;
        }

        IsValidating = true;
        StatusMessage = "Validating...";
        Errors.Clear();
        Warnings.Clear();

        try
        {
            CsvValidationResult result;

            if (Configuration != null)
            {
                result = await _validationService.ValidateCsvAsync(CsvFilePath, Configuration);
            }
            else
            {
                result = await _validationService.ValidateCsvStructureAsync(CsvFilePath);
                if (result.IsValid)
                {
                    var priceResult = await _validationService.ValidatePricesAsync(CsvFilePath);
                    var dupResult = await _validationService.CheckDuplicatesAsync(CsvFilePath);

                    result.Errors.AddRange(priceResult.Errors);
                    result.Errors.AddRange(dupResult.Errors);
                    result.Warnings.AddRange(priceResult.Warnings);
                    result.TotalRows = priceResult.TotalRows;
                    result.ValidRows = priceResult.ValidRows;
                    result.Summary = new CsvValidationSummary
                    {
                        TotalRows = result.TotalRows,
                        ValidRows = result.ValidRows,
                        ErrorCount = result.Errors.Count,
                        WarningCount = result.Warnings.Count
                    };
                }
            }

            ValidationResult = result;
            IsValid = result.IsValid;

            foreach (var error in result.Errors)
            {
                Errors.Add(error);
            }

            foreach (var warning in result.Warnings)
            {
                Warnings.Add(warning);
            }

            OnPropertyChanged(nameof(TotalRows));
            OnPropertyChanged(nameof(ValidRows));
            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(WarningCount));

            if (result.IsValid)
            {
                StatusMessage = $"Validation passed - {result.TotalRows} rows, {result.Warnings.Count} warnings";
            }
            else
            {
                StatusMessage = $"Validation failed - {result.Errors.Count} errors, {result.Warnings.Count} warnings";
            }

            _logger.LogInformation(
                "CSV validation completed: {IsValid} - {ErrorCount} errors, {WarningCount} warnings",
                result.IsValid,
                result.Errors.Count,
                result.Warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate CSV: {FilePath}", CsvFilePath);
            StatusMessage = $"Validation error: {ex.Message}";
            IsValid = false;
        }
        finally
        {
            IsValidating = false;
        }
    }

    private void ExecuteBrowse(object? parameter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Title = "Select CSV File to Validate"
        };

        if (dialog.ShowDialog() == true)
        {
            CsvFilePath = dialog.FileName;
        }
    }

    private void ExecuteClear(object? parameter)
    {
        CsvFilePath = string.Empty;
        Errors.Clear();
        Warnings.Clear();
        ValidationResult = null;
        IsValid = false;
        StatusMessage = string.Empty;
        OnPropertyChanged(nameof(TotalRows));
        OnPropertyChanged(nameof(ValidRows));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
    }
}
