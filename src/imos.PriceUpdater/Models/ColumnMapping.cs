namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Defines the mapping between CSV columns and SQL table columns.
/// </summary>
public sealed class ColumnMapping
{
    /// <summary>
    ///     Gets or sets the name of the SQL table to update.
    /// </summary>
    public string SqlTable { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the name of the column in the CSV file used for searching.
    /// </summary>
    public string CsvSearchColumn { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the name of the column in the CSV file containing the price.
    /// </summary>
    public string CsvPriceColumn { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the name of the column in the SQL table used for searching.
    /// </summary>
    public string SqlSearchColumn { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the name of the column in the SQL table to update with the price.
    /// </summary>
    public string SqlPriceColumn { get; set; } = string.Empty;

    /// <summary>
    ///     Validates that all required mapping properties are set.
    /// </summary>
    /// <returns>True if the mapping is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(SqlTable)
               && !string.IsNullOrWhiteSpace(CsvSearchColumn)
               && !string.IsNullOrWhiteSpace(CsvPriceColumn)
               && !string.IsNullOrWhiteSpace(SqlSearchColumn)
               && !string.IsNullOrWhiteSpace(SqlPriceColumn);
    }
}

