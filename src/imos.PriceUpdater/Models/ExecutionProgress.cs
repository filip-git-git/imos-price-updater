namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents the progress of a price update execution.
/// </summary>
/// <remarks>
///     This record is immutable to ensure reliable test assertions.
/// </remarks>
/// <param name="CurrentRow">The current row being processed.</param>
/// <param name="TotalRows">The total number of rows to process.</param>
/// <param name="Message">An optional status message.</param>
public sealed record ExecutionProgress(int CurrentRow, int TotalRows, string? Message = null)
{
    /// <summary>
    ///     Gets the percentage of progress (0-100).
    /// </summary>
    public int Percentage => TotalRows > 0 ? (int)((double)CurrentRow / TotalRows * 100) : 0;

    /// <summary>
    ///     Gets a value indicating whether the operation is complete.
    /// </summary>
    public bool IsComplete => CurrentRow >= TotalRows;

    /// <summary>
    ///     Gets the current batch number (1-based).
    /// </summary>
    /// <param name="batchSize">The size of each batch.</param>
    /// <returns>The current batch number.</returns>
    public int GetBatchNumber(int batchSize)
    {
        if (batchSize <= 0 || TotalRows == 0)
        {
            return 0;
        }

        return (CurrentRow / batchSize) + 1;
    }

    /// <summary>
    ///     Gets the total number of batches.
    /// </summary>
    /// <param name="batchSize">The size of each batch.</param>
    /// <returns>The total number of batches.</returns>
    public int GetTotalBatches(int batchSize)
    {
        if (batchSize <= 0 || TotalRows == 0)
        {
            return 0;
        }

        return (int)Math.Ceiling((double)TotalRows / batchSize);
    }

    /// <summary>
    ///     Creates an ExecutionProgress from a ProgressEvent.
    /// </summary>
    /// <param name="progressEvent">The source progress event.</param>
    /// <returns>A new ExecutionProgress instance.</returns>
    public static ExecutionProgress FromProgressEvent(ProgressEvent progressEvent)
    {
        ArgumentNullException.ThrowIfNull(progressEvent);
        return new ExecutionProgress(
            progressEvent.CurrentRow,
            progressEvent.TotalRows,
            progressEvent.Message);
    }
}

