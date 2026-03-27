namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents the result of attempting to update a single row.
/// </summary>
/// <remarks>
///     This record is immutable to ensure reliable test assertions.
/// </remarks>
/// <param name="CsvLineNumber">The line number in the CSV file.</param>
/// <param name="SearchValue">The value used to search for the row in the database.</param>
/// <param name="Status">The outcome of the update attempt.</param>
/// <param name="ErrorMessage">An error message if the update failed.</param>
public sealed record UpdateResult(
    int CsvLineNumber,
    string SearchValue,
    UpdateStatus Status,
    string? ErrorMessage = null);

/// <summary>
///     Indicates the outcome of an update operation.
/// </summary>
public enum UpdateStatus
{
    /// <summary>
    ///     The update was successful.
    /// </summary>
    Success,

    /// <summary>
    ///     The row was not found in the database.
    /// </summary>
    Skipped,

    /// <summary>
    ///     The update failed due to an error.
    /// </summary>
    Error
}

