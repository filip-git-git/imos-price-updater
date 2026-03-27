using IMOS.PriceUpdater.Models;
using Microsoft.Data.SqlClient;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Implementation of table schema service using SQL Server information schema.
/// </summary>
public sealed class TableSchemaService : ITableSchemaService
{
    /// <inheritdoc />
    public async Task<TableMappingInfo?> GetTableColumnsAsync(
        SqlConnectionInfo connectionInfo,
        string tableName,
        string schemaName = "dbo",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        var connectionString = connectionInfo.BuildConnectionString();

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var query = @"
                SELECT 
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.IS_NULLABLE,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PRIMARY_KEY
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN (
                    SELECT ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                        AND tc.TABLE_SCHEMA = @SchemaName
                        AND tc.TABLE_NAME = @TableName
                ) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
                WHERE c.TABLE_SCHEMA = @SchemaName
                    AND c.TABLE_NAME = @TableName
                ORDER BY c.ORDINAL_POSITION";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            command.Parameters.AddWithValue("@TableName", tableName);
            command.CommandTimeout = connectionInfo.ConnectionTimeout;

            var columns = new List<ColumnInfo>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add(new ColumnInfo
                {
                    Name = reader.GetString(0),
                    DataType = reader.GetString(1),
                    IsNullable = reader.GetString(2) == "YES",
                    IsPrimaryKey = reader.GetInt32(3) == 1
                });
            }

            if (columns.Count == 0)
            {
                return null;
            }

            return new TableMappingInfo
            {
                TableName = tableName,
                SchemaName = schemaName,
                Columns = columns
            };
        }
        catch (SqlException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TableMappingInfo>> GetUserTablesAsync(
        SqlConnectionInfo connectionInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionInfo);
        var connectionString = connectionInfo.BuildConnectionString();
        var tables = new List<TableMappingInfo>();

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var query = @"
                SELECT 
                    t.TABLE_SCHEMA,
                    t.TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES t
                WHERE t.TABLE_TYPE = 'BASE TABLE'
                    AND t.TABLE_SCHEMA != 'sys'
                ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME";

            await using var command = new SqlCommand(query, connection);
            command.CommandTimeout = connectionInfo.ConnectionTimeout;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(new TableMappingInfo
                {
                    SchemaName = reader.GetString(0),
                    TableName = reader.GetString(1)
                });
            }
        }
        catch (SqlException)
        {
            // Return empty list if we can't get tables
        }

        return tables.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<ColumnMappingValidationResult> ValidateTableColumnsAsync(
        SqlConnectionInfo connectionInfo,
        string tableName,
        IEnumerable<string> requiredColumns,
        string schemaName = "dbo",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionInfo);
        ArgumentNullException.ThrowIfNull(requiredColumns);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        var tableInfo = await GetTableColumnsAsync(
            connectionInfo,
            tableName,
            schemaName,
            cancellationToken);

        if (tableInfo == null)
        {
            return ColumnMappingValidationResult.Failure(
                new[] { $"Table '{schemaName}.{tableName}' does not exist or is not accessible." });
        }

        var errors = new List<string>();
        var columnNames = tableInfo.Columns.Select(c => c.Name.ToLowerInvariant()).ToHashSet();
        var missingColumns = new List<string>();

        foreach (var requiredColumn in requiredColumns)
        {
            if (!columnNames.Contains(requiredColumn.ToLowerInvariant()))
            {
                missingColumns.Add(requiredColumn);
            }
        }

        if (missingColumns.Count > 0)
        {
            errors.Add($"Missing required columns: {string.Join(", ", missingColumns)}");
        }

        return errors.Count == 0
            ? ColumnMappingValidationResult.Success()
            : ColumnMappingValidationResult.Failure(errors);
    }
}

