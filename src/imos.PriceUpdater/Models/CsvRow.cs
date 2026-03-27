namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents a single row from a CSV file with its line number and column values.
/// </summary>
/// <remarks>
///     This record is immutable to ensure reliable test assertions and prevent
///     accidental modification during processing.
/// </remarks>
/// <param name="LineNumber">The 1-based line number in the CSV file.</param>
/// <param name="Values">A dictionary mapping column names to their values.</param>
public sealed record CsvRow(int LineNumber, Dictionary<string, string> Values)
{
    /// <summary>
    ///     Gets the value for a specific column name.
    /// </summary>
    /// <param name="columnName">The name of the column to retrieve.</param>
    /// <returns>The value for the specified column, or null if the column does not exist.</returns>
    public string? GetValue(string columnName)
    {
        return Values.TryGetValue(columnName, out var value) ? value : null;
    }

    /// <summary>
    ///     Tries to get the value for a specific column name.
    /// </summary>
    /// <param name="columnName">The name of the column to retrieve.</param>
    /// <param name="value">When successful, contains the column value.</param>
    /// <returns>True if the column exists; otherwise, false.</returns>
    public bool TryGetValue(string columnName, out string? value)
    {
        return Values.TryGetValue(columnName, out value);
    }
}

