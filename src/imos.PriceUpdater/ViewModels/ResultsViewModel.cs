using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;
using IMOS.PriceUpdater.Models;
using Microsoft.Extensions.Logging;

namespace IMOS.PriceUpdater.ViewModels;

/// <summary>
///     ViewModel for the results view with DataGrid support.
/// </summary>
public sealed class ResultsViewModel : ViewModelBase
{
    private readonly ILogger<ResultsViewModel> _logger;
    private ICollectionView _resultsView;
    private string _searchText = string.Empty;
    private string _statusFilter = "All";
    private ExecutionHistoryDetail? _selectedResult;
    private bool _isDetailDialogOpen;
    private string _detailErrorMessage = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the ResultsViewModel class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ResultsViewModel(ILogger<ResultsViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Results = new ObservableCollection<ExecutionHistoryDetail>();
        _resultsView = CollectionViewSource.GetDefaultView(Results);

        ExportToCsvCommand = new RelayCommand(ExecuteExportToCsv);
        RefreshCommand = new RelayCommand(ExecuteRefresh);
        OpenDetailCommand = new RelayCommand(ExecuteOpenDetail, CanOpenDetail);
        CloseDetailCommand = new RelayCommand(ExecuteCloseDetail);
        CopyCellCommand = new RelayCommand(ExecuteCopyCell);
    }

    /// <summary>
    ///     Gets the collection of results.
    /// </summary>
    public ObservableCollection<ExecutionHistoryDetail> Results { get; }

    /// <summary>
    ///     Gets the filtered view of results.
    /// </summary>
    public ICollectionView ResultsView
    {
        get => _resultsView;
        private set => SetProperty(ref _resultsView, value);
    }

    /// <summary>
    ///     Gets or sets the search text.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the status filter.
    /// </summary>
    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the selected result.
    /// </summary>
    public ExecutionHistoryDetail? SelectedResult
    {
        get => _selectedResult;
        set => SetProperty(ref _selectedResult, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the detail dialog is open.
    /// </summary>
    public bool IsDetailDialogOpen
    {
        get => _isDetailDialogOpen;
        set => SetProperty(ref _isDetailDialogOpen, value);
    }

    /// <summary>
    ///     Gets or sets the detail error message.
    /// </summary>
    public string DetailErrorMessage
    {
        get => _detailErrorMessage;
        set => SetProperty(ref _detailErrorMessage, value);
    }

    /// <summary>
    ///     Gets the count of updated results.
    /// </summary>
    public int UpdatedCount => Results.Count(r => r.Status == ExecutionStatus.Updated);

    /// <summary>
    ///     Gets the count of skipped results.
    /// </summary>
    public int SkippedCount => Results.Count(r => r.Status == ExecutionStatus.Skipped);

    /// <summary>
    ///     Gets the count of error results.
    /// </summary>
    public int ErrorCount => Results.Count(r => r.Status == ExecutionStatus.Error);

    /// <summary>
    ///     Gets the total count.
    /// </summary>
    public int TotalCount => Results.Count;

    /// <summary>
    ///     Gets the export to CSV command.
    /// </summary>
    public ICommand ExportToCsvCommand { get; }

    /// <summary>
    ///     Gets the refresh command.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    ///     Gets the open detail command.
    /// </summary>
    public ICommand OpenDetailCommand { get; }

    /// <summary>
    ///     Gets the close detail command.
    /// </summary>
    public ICommand CloseDetailCommand { get; }

    /// <summary>
    ///     Gets the copy cell command.
    /// </summary>
    public ICommand CopyCellCommand { get; }

    /// <summary>
    ///     Loads results from an execution.
    /// </summary>
    /// <param name="details">The details to load.</param>
    public void LoadResults(IEnumerable<ExecutionHistoryDetail> details)
    {
        ArgumentNullException.ThrowIfNull(details);

        Results.Clear();
        foreach (var detail in details)
        {
            Results.Add(detail);
        }

        ApplyFilters();
        OnPropertyChanged(nameof(UpdatedCount));
        OnPropertyChanged(nameof(SkippedCount));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(TotalCount));

        _logger.LogInformation("Loaded {Count} results", Results.Count);
    }

    /// <summary>
    ///     Clears all results.
    /// </summary>
    public void ClearResults()
    {
        Results.Clear();
        ApplyFilters();
        OnPropertyChanged(nameof(UpdatedCount));
        OnPropertyChanged(nameof(SkippedCount));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(TotalCount));
    }

    /// <summary>
    ///     Applies the current filters to the results view.
    /// </summary>
    private void ApplyFilters()
    {
        if (_resultsView == null)
        {
            return;
        }

        _resultsView.Filter = item =>
        {
            if (item is not ExecutionHistoryDetail detail)
            {
                return false;
            }

            // Apply status filter
            var statusMatch = _statusFilter switch
            {
                "Updated" => detail.Status == ExecutionStatus.Updated,
                "Skipped" => detail.Status == ExecutionStatus.Skipped,
                "Errors" => detail.Status == ExecutionStatus.Error,
                _ => true
            };

            if (!statusMatch)
            {
                return false;
            }

            // Apply search filter
            if (string.IsNullOrWhiteSpace(_searchText))
            {
                return true;
            }

            return detail.MaterialId.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                   detail.SearchTerm.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
        };

        _resultsView.Refresh();
    }

    /// <summary>
    ///     Executes the export to CSV command.
    /// </summary>
    private void ExecuteExportToCsv(object? parameter)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                DefaultExt = ".csv",
                FileName = $"results_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                using var writer = new StreamWriter(dialog.FileName);
                // Write header
                writer.WriteLine("Material ID,Search Term,Old Price,New Price,Status,Error Details,Source Row");

                foreach (var result in Results)
                {
                    var line = $"\"{result.MaterialId}\",\"{result.SearchTerm}\",{result.OldPrice},{result.NewPrice},{result.Status},\"{result.ErrorMessage ?? ""}\",{result.SourceRowNumber}";
                    writer.WriteLine(line);
                }

                _logger.LogInformation("Exported {Count} results to {FilePath}", Results.Count, dialog.FileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export results to CSV");
        }
    }

    /// <summary>
    ///     Executes the refresh command.
    /// </summary>
    private void ExecuteRefresh(object? parameter)
    {
        ApplyFilters();
    }

    /// <summary>
    ///     Determines if detail can be opened.
    /// </summary>
    private bool CanOpenDetail(object? parameter)
    {
        return SelectedResult != null && SelectedResult.Status == ExecutionStatus.Error;
    }

    /// <summary>
    ///     Executes the open detail command.
    /// </summary>
    private void ExecuteOpenDetail(object? parameter)
    {
        if (SelectedResult != null)
        {
            DetailErrorMessage = SelectedResult.ErrorMessage ?? "Unknown error";
            IsDetailDialogOpen = true;
        }
    }

    /// <summary>
    ///     Executes the close detail command.
    /// </summary>
    private void ExecuteCloseDetail(object? parameter)
    {
        IsDetailDialogOpen = false;
    }

    /// <summary>
    ///     Executes the copy cell command.
    /// </summary>
    private void ExecuteCopyCell(object? parameter)
    {
        if (parameter is string value)
        {
            try
            {
                System.Windows.Clipboard.SetText(value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to copy to clipboard");
            }
        }
    }
}
