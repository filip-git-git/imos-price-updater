using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Service for validating CSV files before processing.
/// </summary>
public interface IValidationService
{
    /// <summary>
    ///     Validates a CSV file completely.
    /// </summary>
    /// <param name="csvFilePath">The path to the CSV file.</param>
    /// <param name="config">The price update configuration.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<CsvValidationResult> ValidateCsvAsync(
        string csvFilePath,
        PriceUpdateConfiguration config,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validates the structure of a CSV file (columns only).
    /// </summary>
    /// <param name="csvFilePath">The path to the CSV file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<CsvValidationResult> ValidateCsvStructureAsync(
        string csvFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validates the price values in a CSV file.
    /// </summary>
    /// <param name="csvFilePath">The path to the CSV file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<CsvValidationResult> ValidatePricesAsync(
        string csvFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks for duplicate material IDs in a CSV file.
    /// </summary>
    /// <param name="csvFilePath">The path to the CSV file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<CsvValidationResult> CheckDuplicatesAsync(
        string csvFilePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Determines if validation is required based on file modification time.
    /// </summary>
    /// <param name="csvFilePath">The path to the CSV file.</param>
    /// <param name="lastValidation">The last validation timestamp.</param>
    /// <returns>True if validation is required.</returns>
    bool IsValidationRequired(string csvFilePath, DateTime lastValidation);
}
