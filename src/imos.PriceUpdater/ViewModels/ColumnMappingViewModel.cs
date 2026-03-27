using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;
using Microsoft.Win32;

namespace IMOS.PriceUpdater.ViewModels;

/// <summary>
///     ViewModel for the CSV column to database column mapping view.
/// </summary>
public sealed class ColumnMappingViewModel : ViewModelBase
{
    private readonly ITableSchemaService _schemaService;
    private readonly ICsvParser _csvParser;
    private string? _csvFilePath;
    private string[] _csvHeaders = Array.Empty<string>();
    private List<CsvRow> _previewRows = new();
    private string _selectedTable = string.Empty;
    private string? _selectedSearchColumn;
    private string? _selectedPriceColumn;
    private ObservableCollection<ColumnInfo> _availableColumns = new();
    private ObservableCollection<TableMappingInfo> _availableTables = new();
    private bool _isLoadingTables;
    private bool _isValidMapping;
    private string _validationMessage = string.Empty;
    private ColumnMappingValidationResult? _lastValidationResult;

    public ColumnMappingViewModel() : this(new TableSchemaService(), new CsvParser())
    {
    }

    public ColumnMappingViewModel(ITableSchemaService schemaService, ICsvParser csvParser)
    {
        _schemaService = schemaService;
        _csvParser = csvParser;

        SelectCsvFileCommand = new RelayCommand(SelectCsvFile);
        LoadTablesCommand = new RelayCommand(LoadTables, CanLoadTables);
        ValidateMappingCommand = new RelayCommand(ValidateMapping, CanValidateMapping);
        ClearMappingCommand = new RelayCommand(_ => ClearMapping());
    }

    /// <summary>
    ///     Gets or sets the path to the selected CSV file.
    /// </summary>
    public string? CsvFilePath
    {
        get => _csvFilePath;
        set
        {
            if (SetProperty(ref _csvFilePath, value))
            {
                OnPropertyChanged(nameof(HasFile));
                OnPropertyChanged(nameof(CanValidateMapping));
            }
        }
    }

    /// <summary>
    ///     Gets the CSV headers from the selected file.
    /// </summary>
    public string[] CsvHeaders
    {
        get => _csvHeaders;
        private set => SetProperty(ref _csvHeaders, value);
    }

    /// <summary>
    ///     Gets the preview rows (first 5 rows) from the CSV file.
    /// </summary>
    public List<CsvRow> PreviewRows
    {
        get => _previewRows;
        private set => SetProperty(ref _previewRows, value);
    }

    /// <summary>
    ///     Gets or sets the selected database table name.
    /// </summary>
    public string SelectedTable
    {
        get => _selectedTable;
        set
        {
            if (SetProperty(ref _selectedTable, value))
            {
                OnPropertyChanged(nameof(HasSelectedTable));
                OnPropertyChanged(nameof(CanValidateMapping));
            }
        }
    }

    /// <summary>
    ///     Gets or sets the selected search column name.
    /// </summary>
    public string? SelectedSearchColumn
    {
        get => _selectedSearchColumn;
        set
        {
            if (SetProperty(ref _selectedSearchColumn, value))
            {
                OnPropertyChanged(nameof(CanValidateMapping));
                OnPropertyChanged(nameof(HasValidMapping));
            }
        }
    }

    /// <summary>
    ///     Gets or sets the selected price column name.
    /// </summary>
    public string? SelectedPriceColumn
    {
        get => _selectedPriceColumn;
        set
        {
            if (SetProperty(ref _selectedPriceColumn, value))
            {
                OnPropertyChanged(nameof(CanValidateMapping));
                OnPropertyChanged(nameof(HasValidMapping));
            }
        }
    }

    /// <summary>
    ///     Gets the available columns for the selected table.
    /// </summary>
    public ObservableCollection<ColumnInfo> AvailableColumns
    {
        get => _availableColumns;
        private set => SetProperty(ref _availableColumns, value);
    }

    /// <summary>
    ///     Gets the available tables in the database.
    /// </summary>
    public ObservableCollection<TableMappingInfo> AvailableTables
    {
        get => _availableTables;
        private set => SetProperty(ref _availableTables, value);
    }

    /// <summary>
    ///     Gets or sets whether tables are being loaded.
    /// </summary>
    /// <remarks>
    ///     Setter is internal for testing purposes.
    /// </remarks>
    public bool IsLoadingTables
    {
        get => _isLoadingTables;
        internal set => SetProperty(ref _isLoadingTables, value);
    }

    /// <summary>
    ///     Gets a value indicating whether a CSV file has been selected.
    /// </summary>
    public bool HasFile => !string.IsNullOrEmpty(CsvFilePath);

    /// <summary>
    ///     Gets a value indicating whether a table has been selected.
    /// </summary>
    public bool HasSelectedTable => !string.IsNullOrEmpty(SelectedTable);

    /// <summary>
    ///     Gets a value indicating whether the current mapping is valid.
    /// </summary>
    public bool IsValidMapping
    {
        get => _isValidMapping;
        private set
        {
            if (SetProperty(ref _isValidMapping, value))
            {
                OnPropertyChanged(nameof(HasValidMapping));
            }
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the user has made a valid mapping selection.
    /// </summary>
    public bool HasValidMapping => !string.IsNullOrEmpty(SelectedSearchColumn)
                                   && !string.IsNullOrEmpty(SelectedPriceColumn);

    /// <summary>
    ///     Gets or sets the validation message.
    /// </summary>
    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    /// <summary>
    ///     Gets or sets the last validation result.
    /// </summary>
    public ColumnMappingValidationResult? LastValidationResult
    {
        get => _lastValidationResult;
        private set => SetProperty(ref _lastValidationResult, value);
    }

    /// <summary>
    ///     Gets the select CSV file command.
    /// </summary>
    public ICommand SelectCsvFileCommand { get; }

    /// <summary>
    ///     Gets the load tables command.
    /// </summary>
    public ICommand LoadTablesCommand { get; }

    /// <summary>
    ///     Gets the validate mapping command.
    /// </summary>
    public ICommand ValidateMappingCommand { get; }

    /// <summary>
    ///     Gets the clear mapping command.
    /// </summary>
    public ICommand ClearMappingCommand { get; }

    /// <summary>
    ///     Gets the connection info from the parent view model.
    /// </summary>
    public SqlConnectionInfo? ConnectionInfo { get; set; }

    private bool CanLoadTables(object? parameter)
    {
        return ConnectionInfo != null && !IsLoadingTables;
    }

    /// <summary>
    ///     Determines whether the validate mapping command can execute.
    /// </summary>
    /// <param name="parameter">The command parameter.</param>
    /// <returns>True if mapping can be validated; otherwise, false.</returns>
    public bool CanValidateMapping(object? parameter)
    {
        return HasFile && HasSelectedTable && HasValidMapping;
    }

    private async void LoadTables(object? parameter)
    {
        if (ConnectionInfo == null)
        {
            return;
        }

        IsLoadingTables = true;
        AvailableTables.Clear();

        try
        {
            var tables = await _schemaService.GetUserTablesAsync(ConnectionInfo);
            foreach (var table in tables)
            {
                AvailableTables.Add(table);
            }
        }
        finally
        {
            IsLoadingTables = false;
        }
    }

    private async void ValidateMapping(object? parameter)
    {
        if (ConnectionInfo == null || string.IsNullOrEmpty(SelectedTable))
        {
            return;
        }

        var mappingInfo = await _schemaService.GetTableColumnsAsync(
            ConnectionInfo,
            SelectedTable);

        if (mappingInfo == null)
        {
            ValidationMessage = "Could not retrieve table columns.";
            IsValidMapping = false;
            return;
        }

        var errors = new List<string>();

        // Validate search column
        var searchColumn = mappingInfo.Columns.FirstOrDefault(
            c => c.Name.Equals(SelectedSearchColumn, StringComparison.OrdinalIgnoreCase));

        if (searchColumn == null)
        {
            errors.Add($"Search column '{SelectedSearchColumn}' not found in table.");
        }
        else if (!searchColumn.IsSearchable)
        {
            errors.Add($"Search column '{SelectedSearchColumn}' is not a searchable type.");
        }

        // Validate price column
        var priceColumn = mappingInfo.Columns.FirstOrDefault(
            c => c.Name.Equals(SelectedPriceColumn, StringComparison.OrdinalIgnoreCase));

        if (priceColumn == null)
        {
            errors.Add($"Price column '{SelectedPriceColumn}' not found in table.");
        }
        else if (!priceColumn.IsPriceType)
        {
            errors.Add($"Price column '{SelectedPriceColumn}' is not a numeric/price type.");
        }

        // Check that search and price columns are different
        if (SelectedSearchColumn?.Equals(SelectedPriceColumn, StringComparison.OrdinalIgnoreCase) == true)
        {
            errors.Add("Search and price columns must be different.");
        }

        LastValidationResult = errors.Count == 0
            ? ColumnMappingValidationResult.Success()
            : ColumnMappingValidationResult.Failure(errors);

        IsValidMapping = errors.Count == 0;
        ValidationMessage = errors.Count == 0
            ? "Mapping is valid."
            : string.Join(Environment.NewLine, errors);
    }

    /// <summary>
    ///     Handles the CSV file selection.
    /// </summary>
    private async void SelectCsvFile(object? parameter)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select CSV File",
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadCsvFileAsync(dialog.FileName);
        }
    }

    /// <summary>
    ///     Loads a CSV file and parses its headers.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    public async Task LoadCsvFileAsync(string filePath)
    {
        try
        {
            CsvFilePath = filePath;
            PreviewRows.Clear();

            var headers = new List<string>();
            var previewRows = new List<CsvRow>();
            var rowCount = 0;

            await foreach (var row in _csvParser.ParseAsync(filePath))
            {
                if (rowCount == 0)
                {
                    headers.AddRange(row.Values.Keys);
                }
                else if (rowCount <= 5)
                {
                    previewRows.Add(row);
                }

                rowCount++;

                // Stop after reading enough for preview
                if (rowCount > 6)
                {
                    break;
                }
            }

            if (headers.Count == 0)
            {
                CsvHeaders = Array.Empty<string>();
                ValidationMessage = "CSV file is empty or has no headers.";
                return;
            }

            CsvHeaders = headers.ToArray();
            PreviewRows = previewRows;
            ValidationMessage = $"Loaded {headers.Count} columns, {rowCount - 1} rows.";
        }
        catch (Exception ex)
        {
            CsvHeaders = Array.Empty<string>();
            PreviewRows.Clear();
            ValidationMessage = $"Error loading CSV file: {ex.Message}";
        }
    }

    /// <summary>
    ///     Clears the current mapping configuration.
    /// </summary>
    public void ClearMapping()
    {
        CsvFilePath = null;
        CsvHeaders = Array.Empty<string>();
        PreviewRows.Clear();
        SelectedTable = string.Empty;
        SelectedSearchColumn = null;
        SelectedPriceColumn = null;
        AvailableColumns.Clear();
        IsValidMapping = false;
        ValidationMessage = string.Empty;
        LastValidationResult = null;
    }
}

