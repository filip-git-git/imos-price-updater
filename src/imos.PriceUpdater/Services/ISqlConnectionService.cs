using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Service for managing SQL Server database connections.
/// </summary>
public interface ISqlConnectionService
{
    /// <summary>
    ///     Tests the connection to the database using the provided configuration.
    /// </summary>
    /// <param name="connectionInfo">The connection information.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A connection test result indicating success or failure.</returns>
    Task<ConnectionTestResult> TestConnectionAsync(
        SqlConnectionInfo connectionInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the list of available databases on the server.
    /// </summary>
    /// <param name="connectionInfo">The connection information (without database).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of database names.</returns>
    Task<IReadOnlyList<string>> GetDatabasesAsync(
        SqlConnectionInfo connectionInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validates the connection information without actually connecting.
    /// </summary>
    /// <param name="connectionInfo">The connection information to validate.</param>
    /// <returns>A validation result.</returns>
    ValidationResult ValidateConnectionInfo(SqlConnectionInfo connectionInfo);
}

