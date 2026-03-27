using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents the overall summary of a price update execution.
/// </summary>
public sealed class ExecutionSummary
{
    /// <summary>
    ///     Gets the total number of rows processed.
    /// </summary>
    public int TotalRows { get; }

    /// <summary>
    ///     Gets the number of rows successfully updated.
    /// </summary>
    public int UpdatedCount { get; }

    /// <summary>
    ///     Gets the number of rows skipped (not found in database).
    /// </summary>
    public int SkippedCount { get; }

    /// <summary>
    ///     Gets the number of rows that failed with errors.
    /// </summary>
    public int ErrorCount { get; }

    /// <summary>
    ///     Gets when the execution started.
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    ///     Gets when the execution ended.
    /// </summary>
    public DateTime EndTime { get; }

    /// <summary>
    ///     Gets the duration of the execution in seconds.
    /// </summary>
    public double DurationSeconds => (EndTime - StartTime).TotalSeconds;

    /// <summary>
    ///     Gets the success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate => TotalRows > 0 ? (double)UpdatedCount / TotalRows * 100 : 0;

    /// <summary>
    ///     Gets a value indicating whether any errors occurred during execution.
    /// </summary>
    public bool HasErrors => ErrorCount > 0;

    /// <summary>
    ///     Gets the collection of individual update results.
    /// </summary>
    public List<UpdateResult> Results { get; } = new();

    /// <summary>
    ///     Gets the collection of errors that occurred during execution.
    /// </summary>
    public List<ExecutionError> Errors { get; } = new();

    /// <summary>
    ///     Initializes a new instance of the ExecutionSummary class.
    /// </summary>
    public ExecutionSummary(
        int totalRows,
        int updatedCount,
        int skippedCount,
        int errorCount,
        DateTime startTime,
        DateTime endTime)
    {
        TotalRows = totalRows;
        UpdatedCount = updatedCount;
        SkippedCount = skippedCount;
        ErrorCount = errorCount;
        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>
    ///     Creates a copy of this summary with modified end time.
    /// </summary>
    /// <param name="endTime">The new end time.</param>
    /// <returns>A new ExecutionSummary with the specified end time.</returns>
    public ExecutionSummary WithEndTime(DateTime endTime)
    {
        var newSummary = new ExecutionSummary(
            TotalRows,
            UpdatedCount,
            SkippedCount,
            ErrorCount,
            StartTime,
            endTime);

        // Copy errors to the new instance
        foreach (var error in Errors)
        {
            newSummary.AddError(error);
        }

        // Copy results to the new instance
        foreach (var result in Results)
        {
            newSummary.AddResult(result);
        }

        return newSummary;
    }

    /// <summary>
    ///     Adds an individual update result to the collection.
    /// </summary>
    /// <param name="result">The result to add.</param>
    public void AddResult(UpdateResult result)
    {
        Results.Add(result);
    }

    /// <summary>
    ///     Adds an execution error to the collection.
    /// </summary>
    /// <param name="error">The error to add.</param>
    public void AddError(ExecutionError error)
    {
        Errors.Add(error);
    }
}

/// <summary>
///     Represents a detailed error that occurred during execution.
/// </summary>
/// <param name="RowNumber">The CSV row number where the error occurred.</param>
/// <param name="SearchValue">The search value being used.</param>
/// <param name="ErrorMessage">The error message.</param>
/// <param name="ExceptionType">The type of exception that occurred.</param>
public sealed record ExecutionError(
    int RowNumber,
    string SearchValue,
    string ErrorMessage,
    string? ExceptionType = null);

