namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents a progress update during a long-running operation.
/// </summary>
/// <remarks>
///     This record is immutable to ensure reliable test assertions.
/// </remarks>
/// <param name="CurrentRow">The current row being processed.</param>
/// <param name="TotalRows">The total number of rows to process.</param>
/// <param name="Message">An optional status message.</param>
public sealed record ProgressEvent(int CurrentRow, int TotalRows, string? Message = null)
{
    /// <summary>
    ///     Gets the percentage of progress (0-100).
    /// </summary>
    public int Percentage => TotalRows > 0 ? (int)((double)CurrentRow / TotalRows * 100) : 0;

    /// <summary>
    ///     Gets a value indicating whether the operation is complete.
    /// </summary>
    public bool IsComplete => CurrentRow >= TotalRows;
}

