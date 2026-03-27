namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents a history record of a price update execution.
/// </summary>
public sealed class ExecutionHistory
{
    /// <summary>
    ///     Gets the unique identifier for this execution record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets when the execution was performed.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    ///     Gets the name of the CSV file used.
    /// </summary>
    public string CsvFileName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the full path to the CSV file.
    /// </summary>
    public string CsvFilePath { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the name of the configuration file used.
    /// </summary>
    public string ConfigFileName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the outcome of the execution.
    /// </summary>
    public ExecutionOutcome Outcome { get; set; }

    /// <summary>
    ///     Gets the total number of rows processed.
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    ///     Gets the number of rows successfully updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    ///     Gets the number of rows skipped.
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    ///     Gets the number of rows that resulted in errors.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    ///     Gets the duration of the execution in seconds.
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    ///     Gets a summary of any errors that occurred.
    /// </summary>
    public string? ErrorSummary { get; set; }

    /// <summary>
    ///     Gets the parent execution ID if this was a re-run.
    /// </summary>
    public Guid? ParentExecutionId { get; set; }

    /// <summary>
    ///     Gets the collection of detailed results for this execution.
    /// </summary>
    public List<ExecutionHistoryDetail> Details { get; set; } = new();

    /// <summary>
    ///     Initializes a new instance of the ExecutionHistory class.
    /// </summary>
    public ExecutionHistory()
    {
        Id = Guid.NewGuid();
        ExecutedAt = DateTime.UtcNow;
    }
}
