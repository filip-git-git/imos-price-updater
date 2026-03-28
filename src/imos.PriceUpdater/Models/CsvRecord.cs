namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents a parsed and validated CSV record ready for price update.
/// </summary>
/// <remarks>
///     This record is immutable to ensure reliable test assertions and prevent
///     accidental modification during processing.
/// </remarks>
/// <param name="RowNumber">The 1-based line number in the CSV file.</param>
/// <param name="SearchTerm">The value used to search for the material in the database.</param>
/// <param name="Price">The new price value to set.</param>
/// <param name="IsValid">Whether the record passed validation.</param>
public sealed record CsvRecord(
    int RowNumber,
    string SearchTerm,
    decimal Price,
    bool IsValid)
{
    /// <summary>
    ///     Creates a CsvRecord from a CsvRow and column mapping.
    /// </summary>
    /// <param name="csvRow">The source CSV row.</param>
    /// <param name="mapping">The column mapping configuration.</param>
    /// <returns>A new CsvRecord instance.</returns>
    public static CsvRecord FromCsvRow(CsvRow csvRow, ColumnMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(csvRow);
        ArgumentNullException.ThrowIfNull(mapping);

        // Try to get the search value
        if (!csvRow.TryGetValue(mapping.CsvSearchColumn, out var searchValue)
            || string.IsNullOrWhiteSpace(searchValue))
        {
            return new CsvRecord(csvRow.LineNumber, string.Empty, 0m, false);
        }

        // Try to parse the price
        if (!csvRow.TryGetValue(mapping.CsvPriceColumn, out var priceValue)
            || string.IsNullOrWhiteSpace(priceValue))
        {
            return new CsvRecord(csvRow.LineNumber, searchValue, 0m, false);
        }

        // Try to convert price (handles both . and , as decimal separator)
        if (!TryParsePrice(priceValue, out var price))
        {
            return new CsvRecord(csvRow.LineNumber, searchValue, 0m, false);
        }

        return new CsvRecord(csvRow.LineNumber, searchValue, price, true);
    }

    /// <summary>
    ///     Attempts to parse a price value from string, handling various formats.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="price">When successful, contains the parsed price.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    private static bool TryParsePrice(string value, out decimal price)
    {
        price = 0m;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Remove any whitespace
        value = value.Trim();

        // Try direct decimal parsing first
        if (decimal.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out price))
        {
            return true;
        }

        // Try with comma as decimal separator
        if (decimal.TryParse(value, System.Globalization.NumberStyles.Any,
            new System.Globalization.CultureInfo("de-DE"), out price))
        {
            return true;
        }

        // Try with Polish locale (comma as decimal separator)
        if (decimal.TryParse(value, System.Globalization.NumberStyles.Any,
            new System.Globalization.CultureInfo("pl-PL"), out price))
        {
            return true;
        }

        return false;
    }
}

