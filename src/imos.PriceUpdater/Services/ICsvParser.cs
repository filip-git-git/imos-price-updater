using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Interface for CSV parsing operations.
/// </summary>
public interface ICsvParser
{
    /// <summary>
    ///     Parses a CSV file and returns the rows as CsvRow objects.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An enumerable of CsvRow objects.</returns>
    IAsyncEnumerable<CsvRow> ParseAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Parses a CSV file in batches.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    /// <param name="batchSize">The number of rows per batch.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An async enumerable of batches of CsvRow objects.</returns>
    IAsyncEnumerable<CsvRow[]> ParseInBatchesAsync(string filePath, int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the detected encoding of a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The detected encoding.</returns>
    System.Text.Encoding DetectEncoding(string filePath);

    /// <summary>
    ///     Validates that a CSV file has the required columns.
    /// </summary>
    /// <param name="filePath">The path to the CSV file.</param>
    /// <param name="requiredColumns">The required column names.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if all required columns are present; otherwise, false.</returns>
    Task<bool> ValidateColumnsAsync(string filePath, string[] requiredColumns, CancellationToken cancellationToken = default);
}

