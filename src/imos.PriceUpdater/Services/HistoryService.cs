using System.IO;
using System.Text.Json;
using IMOS.PriceUpdater.Models;
using Microsoft.Extensions.Logging;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Implementation of the history service that stores execution history as JSON files.
/// </summary>
public sealed class HistoryService : IHistoryService
{
    private readonly ILogger<HistoryService> _logger;
    private readonly string _historyDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    ///     Initializes a new instance of the HistoryService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public HistoryService(ILogger<HistoryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _historyDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "imosPriceUpdater",
            "history");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        EnsureDirectoryExists();
    }

    /// <inheritdoc />
    public async Task<List<ExecutionHistory>> GetHistoryAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        ExecutionOutcome? outcome = null,
        CancellationToken cancellationToken = default)
    {
        var allHistory = await LoadAllHistoryAsync(cancellationToken);

        var filtered = allHistory
            .Where(h => !fromDate.HasValue || h.ExecutedAt >= fromDate.Value)
            .Where(h => !toDate.HasValue || h.ExecutedAt <= toDate.Value)
            .Where(h => !outcome.HasValue || h.Outcome == outcome.Value)
            .OrderByDescending(h => h.ExecutedAt)
            .ToList();

        return filtered;
    }

    /// <inheritdoc />
    public async Task<ExecutionHistory?> GetHistoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filePath = GetHistoryFilePath(id);
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("History entry not found: {HistoryId}", id);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var history = JsonSerializer.Deserialize<ExecutionHistory>(json, _jsonOptions);
            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load history entry: {HistoryId}", id);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<ExecutionHistoryDetail>> GetHistoryDetailsAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var history = await GetHistoryByIdAsync(executionId, cancellationToken);
        return history?.Details ?? new List<ExecutionHistoryDetail>();
    }

    /// <inheritdoc />
    public async Task SaveExecutionAsync(ExecutionHistory history, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(history);

        var filePath = GetHistoryFilePath(history.Id);

        try
        {
            // Save main history
            var json = JsonSerializer.Serialize(history, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.LogInformation(
                "Saved execution history: {HistoryId} - {CsvFileName}",
                history.Id,
                history.CsvFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save execution history: {HistoryId}", history.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SaveExecutionDetailsAsync(
        Guid executionId,
        List<ExecutionHistoryDetail> details,
        CancellationToken cancellationToken = default)
    {
        var history = await GetHistoryByIdAsync(executionId, cancellationToken);
        if (history == null)
        {
            _logger.LogWarning("Cannot save details: history entry not found: {HistoryId}", executionId);
            return;
        }

        history.Details = details;
        await SaveExecutionAsync(history, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteHistoryEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filePath = GetHistoryFilePath(id);
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("History entry not found for deletion: {HistoryId}", id);
            return;
        }

        try
        {
            await Task.Run(() => File.Delete(filePath), cancellationToken);
            _logger.LogInformation("Deleted history entry: {HistoryId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete history entry: {HistoryId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetHistoryCountAsync(CancellationToken cancellationToken = default)
    {
        var allHistory = await LoadAllHistoryAsync(cancellationToken);
        return allHistory.Count;
    }

    /// <inheritdoc />
    public async Task CleanupOldHistoryAsync(int keepCount, CancellationToken cancellationToken = default)
    {
        if (keepCount <= 0)
        {
            throw new ArgumentException("Keep count must be greater than zero.", nameof(keepCount));
        }

        var allHistory = await LoadAllHistoryAsync(cancellationToken);
        var sortedHistory = allHistory.OrderByDescending(h => h.ExecutedAt).ToList();

        if (sortedHistory.Count <= keepCount)
        {
            return;
        }

        var toDelete = sortedHistory.Skip(keepCount).ToList();
        foreach (var history in toDelete)
        {
            await DeleteHistoryEntryAsync(history.Id, cancellationToken);
        }

        _logger.LogInformation("Cleaned up {Count} old history entries", toDelete.Count);
    }

    /// <summary>
    ///     Gets the file path for a history entry.
    /// </summary>
    private string GetHistoryFilePath(Guid id)
    {
        return Path.Combine(_historyDirectory, $"history_{id}.json");
    }

    /// <summary>
    ///     Ensures the history directory exists.
    /// </summary>
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_historyDirectory))
        {
            Directory.CreateDirectory(_historyDirectory);
            _logger.LogDebug("Created history directory: {Directory}", _historyDirectory);
        }
    }

    /// <summary>
    ///     Loads all history entries from the directory.
    /// </summary>
    private async Task<List<ExecutionHistory>> LoadAllHistoryAsync(CancellationToken cancellationToken)
    {
        var historyList = new List<ExecutionHistory>();

        if (!Directory.Exists(_historyDirectory))
        {
            return historyList;
        }

        var files = Directory.GetFiles(_historyDirectory, "history_*.json");
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var history = JsonSerializer.Deserialize<ExecutionHistory>(json, _jsonOptions);
                if (history != null)
                {
                    historyList.Add(history);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load history file: {File}", file);
            }
        }

        return historyList;
    }
}
