using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Service for executing price updates from CSV files to SQL Server database.
/// </summary>
public interface IPriceUpdateService
{
    /// <summary>
    ///     Executes a price update operation using the specified CSV file and configuration.
    /// </summary>
    /// <param name="configuration">The price update configuration.</param>
    /// <param name="csvFilePath">Path to the CSV file containing price data.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>An execution summary with results and statistics.</returns>
    Task<ExecutionSummary> ExecuteUpdateAsync(
        PriceUpdateConfiguration configuration,
        string csvFilePath,
        IProgress<ExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validates the configuration before execution.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>A validation result indicating success or failure with error messages.</returns>
    ValidationResult ValidateConfiguration(PriceUpdateConfiguration configuration);

    /// <summary>
    ///     Gets the schema of a table for validation purposes.
    /// </summary>
    /// <param name="connectionInfo">The connection information.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="schemaName">The schema name (defaults to "dbo").</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Table mapping information, or null if the table doesn't exist.</returns>
    Task<TableMappingInfo?> GetTableSchemaAsync(
        SqlConnectionInfo connectionInfo,
        string tableName,
        string schemaName = "dbo",
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Counts the number of data rows in a CSV file.
    /// </summary>
    /// <param name="csvFilePath">Path to the CSV file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of data rows (excluding header).</returns>
    Task<int> CountCsvRowsAsync(string csvFilePath, CancellationToken cancellationToken = default);
}

