namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents a single column mapping between a CSV column and a database column.
/// </summary>
public sealed class ColumnMap
{
    /// <summary>
    ///     Gets or sets the name of the column in the CSV file.
    /// </summary>
    public string CsvColumnName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the name of the column in the SQL table.
    /// </summary>
    public string SqlColumnName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the data type of the SQL column.
    /// </summary>
    public string SqlDataType { get; set; } = string.Empty;
}

