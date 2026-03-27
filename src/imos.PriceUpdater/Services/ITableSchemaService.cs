using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Service for reading database table schema information.
/// </summary>
public interface ITableSchemaService
{
    /// <summary>
    ///     Gets the column information for a specified table.
    /// </summary>
    /// <param name="connectionInfo">The connection information.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="schemaName">The schema name (defaults to "dbo").</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A table mapping info containing column details.</returns>
    Task<TableMappingInfo?> GetTableColumnsAsync(
        SqlConnectionInfo connectionInfo,
        string tableName,
        string schemaName = "dbo",
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a list of user tables in the database.
    /// </summary>
    /// <param name="connectionInfo">The connection information.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of table names with their schemas.</returns>
    Task<IReadOnlyList<TableMappingInfo>> GetUserTablesAsync(
        SqlConnectionInfo connectionInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validates that a table exists and has the required columns.
    /// </summary>
    /// <param name="connectionInfo">The connection information.</param>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="requiredColumns">The required column names.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A validation result indicating if all columns exist.</returns>
    Task<ColumnMappingValidationResult> ValidateTableColumnsAsync(
        SqlConnectionInfo connectionInfo,
        string tableName,
        IEnumerable<string> requiredColumns,
        string schemaName = "dbo",
        CancellationToken cancellationToken = default);
}

