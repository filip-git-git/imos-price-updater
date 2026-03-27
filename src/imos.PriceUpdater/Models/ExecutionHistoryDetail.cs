namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents a detailed record of a single row update within an execution.
/// </summary>
public sealed class ExecutionHistoryDetail
{
    /// <summary>
    ///     Gets or sets the execution ID this detail belongs to.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    ///     Gets or sets the material ID from the CSV.
    /// </summary>
    public string MaterialId { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the search term used to find the row.
    /// </summary>
    public string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the old price value (null if not found).
    /// </summary>
    public decimal? OldPrice { get; set; }

    /// <summary>
    ///     Gets or sets the new price value from the CSV.
    /// </summary>
    public decimal? NewPrice { get; set; }

    /// <summary>
    ///     Gets or sets the status of this update.
    /// </summary>
    public ExecutionStatus Status { get; set; }

    /// <summary>
    ///     Gets or sets the error message if the update failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Gets or sets the row number in the source CSV file.
    /// </summary>
    public int SourceRowNumber { get; set; }
}
