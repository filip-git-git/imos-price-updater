using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;
using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;
using Microsoft.Extensions.Logging;

namespace IMOS.PriceUpdater.ViewModels;

/// <summary>
///     ViewModel for the execution history view.
/// </summary>
public sealed class HistoryViewModel : ViewModelBase
{
    private readonly IHistoryService _historyService;
    private readonly ILogger<HistoryViewModel> _logger;
    private ICollectionView _historyView;
    private ExecutionHistory? _selectedHistory;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private string _outcomeFilter = "All";
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the HistoryViewModel class.
    /// </summary>
    /// <param name="historyService">The history service.</param>
    /// <param name="logger">The logger instance.</param>
    public HistoryViewModel(IHistoryService historyService, ILogger<HistoryViewModel> logger)
    {
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        HistoryItems = new ObservableCollection<ExecutionHistory>();
        _historyView = CollectionViewSource.GetDefaultView(HistoryItems);

        RefreshCommand = new RelayCommand(async _ => await ExecuteRefreshAsync());
        DeleteCommand = new RelayCommand(async _ => await ExecuteDeleteAsync(), CanExecuteDelete);
        ExportReportCommand = new RelayCommand(async _ => await ExecuteExportReportAsync());
        ReRunCommand = new RelayCommand(async _ => await ExecuteReRunAsync(), CanExecuteReRun);
        ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
    }

    /// <summary>
    ///     Gets the collection of history items.
    /// </summary>
    public ObservableCollection<ExecutionHistory> HistoryItems { get; }

    /// <summary>
    ///     Gets the filtered view of history.
    /// </summary>
    public ICollectionView HistoryView
    {
        get => _historyView;
        private set => SetProperty(ref _historyView, value);
    }

    /// <summary>
    ///     Gets or sets the selected history item.
    /// </summary>
    public ExecutionHistory? SelectedHistory
    {
        get => _selectedHistory;
        set
        {
            if (SetProperty(ref _selectedHistory, value))
            {
                CommandManager.InvalidateRequerySuggested();
                LoadDetailsAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    ///     Gets or sets the from date filter.
    /// </summary>
    public DateTime? FromDate
    {
        get => _fromDate;
        set
        {
            if (SetProperty(ref _fromDate, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the to date filter.
    /// </summary>
    public DateTime? ToDate
    {
        get => _toDate;
        set
        {
            if (SetProperty(ref _toDate, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the outcome filter.
    /// </summary>
    public string OutcomeFilter
    {
        get => _outcomeFilter;
        set
        {
            if (SetProperty(ref _outcomeFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the loading indicator.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
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
    ///     Gets the details of the selected history item.
    /// </summary>
    public ObservableCollection<ExecutionHistoryDetail> SelectedDetails { get; } = new();

    /// <summary>
    ///     Gets the refresh command.
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    ///     Gets the delete command.
    /// </summary>
    public ICommand DeleteCommand { get; }

    /// <summary>
    ///     Gets the export report command.
    /// </summary>
    public ICommand ExportReportCommand { get; }

    /// <summary>
    ///     Gets the re-run command.
    /// </summary>
    public ICommand ReRunCommand { get; }

    /// <summary>
    ///     Gets the clear filters command.
    /// </summary>
    public ICommand ClearFiltersCommand { get; }

    /// <summary>
    ///     Loads the history asynchronously.
    /// </summary>
    public async Task LoadHistoryAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading history...";

        try
        {
            var outcome = _outcomeFilter switch
            {
                "Completed" => ExecutionOutcome.Success,
                "Failed" => ExecutionOutcome.Failed,
                "Cancelled" => ExecutionOutcome.Cancelled,
                _ => (ExecutionOutcome?)null
            };

            var history = await _historyService.GetHistoryAsync(_fromDate, _toDate, outcome);

            HistoryItems.Clear();
            foreach (var item in history)
            {
                HistoryItems.Add(item);
            }

            ApplyFilters();
            StatusMessage = $"Loaded {HistoryItems.Count} entries";
            _logger.LogInformation("Loaded {Count} history entries", HistoryItems.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load history");
            StatusMessage = "Failed to load history";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    ///     Applies the current filters to the history view.
    /// </summary>
    private void ApplyFilters()
    {
        if (_historyView == null) return;

        _historyView.Filter = item =>
        {
            if (item is not ExecutionHistory history)
            {
                return false;
            }

            // Apply outcome filter
            var outcomeMatch = _outcomeFilter switch
            {
                "Completed" => history.Outcome == ExecutionOutcome.Success,
                "Failed" => history.Outcome == ExecutionOutcome.Failed,
                "Cancelled" => history.Outcome == ExecutionOutcome.Cancelled,
                _ => true
            };

            return outcomeMatch;
        };

        _historyView.Refresh();
    }

    private async Task LoadDetailsAsync()
    {
        if (SelectedHistory == null)
        {
            SelectedDetails.Clear();
            return;
        }

        try
        {
            var details = await _historyService.GetHistoryDetailsAsync(SelectedHistory.Id);
            SelectedDetails.Clear();
            foreach (var detail in details)
            {
                SelectedDetails.Add(detail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load history details");
        }
    }

    private async Task ExecuteRefreshAsync()
    {
        await LoadHistoryAsync();
    }

    private bool CanExecuteDelete(object? parameter)
    {
        return SelectedHistory != null;
    }

    private async Task ExecuteDeleteAsync()
    {
        if (SelectedHistory == null) return;

        try
        {
            await _historyService.DeleteHistoryEntryAsync(SelectedHistory.Id);
            HistoryItems.Remove(SelectedHistory);
            SelectedHistory = null;
            StatusMessage = "Entry deleted";
            _logger.LogInformation("Deleted history entry");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete history entry");
            StatusMessage = "Failed to delete entry";
        }
    }

    private async Task ExecuteExportReportAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                DefaultExt = ".csv",
                FileName = $"history_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                await using var writer = new StreamWriter(dialog.FileName);
                await writer.WriteLineAsync("Date/Time,CSV File,Outcome,Total Rows,Updated,Skipped,Errors,Duration (s)");

                foreach (var item in HistoryItems)
                {
                    var line = $"{item.ExecutedAt:yyyy-MM-dd HH:mm:ss},\"{item.CsvFileName}\",{item.Outcome},{item.TotalRows},{item.UpdatedCount},{item.SkippedCount},{item.ErrorCount},{item.DurationSeconds}";
                    await writer.WriteLineAsync(line);
                }

                StatusMessage = $"Exported {HistoryItems.Count} entries";
                _logger.LogInformation("Exported history report to {FilePath}", dialog.FileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export history report");
            StatusMessage = "Failed to export report";
        }
    }

    private bool CanExecuteReRun(object? parameter)
    {
        return SelectedHistory != null && File.Exists(SelectedHistory.CsvFilePath);
    }

    private async Task ExecuteReRunAsync()
    {
        if (SelectedHistory == null) return;

        _logger.LogInformation("Re-run requested for execution {ExecutionId}", SelectedHistory.Id);
        StatusMessage = "Loading configuration for re-run...";

        // This would typically navigate back to the main view with pre-filled configuration
        // For now, we just log the action
        await Task.CompletedTask;
    }

    private void ExecuteClearFilters(object? parameter)
    {
        FromDate = null;
        ToDate = null;
        OutcomeFilter = "All";
        ApplyFilters();
    }
}
