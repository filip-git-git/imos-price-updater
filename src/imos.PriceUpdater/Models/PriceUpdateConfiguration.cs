using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Configuration for the price update operation.
/// </summary>
public sealed class PriceUpdateConfiguration
{
    /// <summary>
    ///     Gets or sets the SQL Server connection information.
    /// </summary>
    public SqlConnectionInfo? SqlConnection { get; set; }

    /// <summary>
    ///     Gets or sets the column mapping between CSV and database.
    /// </summary>
    public ColumnMapping? ColumnMapping { get; set; }

    /// <summary>
    ///     Gets or sets the number of rows to process in each batch.
    /// </summary>
    /// <remarks>
    ///     Default is 1000 rows as per requirements.
    /// </remarks>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    ///     Gets or sets the command timeout in seconds for each batch operation.
    /// </summary>
    /// <remarks>
    ///     Default is 30 seconds as per requirements.
    /// </remarks>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    ///     Gets or sets the maximum number of retry attempts for transient failures.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    ///     Gets or sets the delay in milliseconds between retry attempts.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    ///     Validates that all required configuration properties are set.
    /// </summary>
    /// <returns>True if the configuration is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        if (SqlConnection == null || !SqlConnection.Validate().IsValid)
        {
            return false;
        }

        if (ColumnMapping == null || !ColumnMapping.IsValid())
        {
            return false;
        }

        if (BatchSize <= 0)
        {
            return false;
        }

        if (CommandTimeout <= 0)
        {
            return false;
        }

        return true;
    }
}

