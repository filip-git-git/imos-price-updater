using System.Data;
using System.Runtime.CompilerServices;
using IMOS.PriceUpdater.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Implementation of the price update service that processes CSV files and updates
///     material prices in SQL Server database using batch processing with transaction support.
/// </summary>
public sealed class PriceUpdateService : IPriceUpdateService
{
    private readonly ICsvParser _csvParser;
    private readonly ILogger<PriceUpdateService> _logger;

    // Transient SQL error numbers that warrant a retry
    private const int ServerNotFoundErrorNumber = 2;
    private const int TimeoutErrorNumber = -2;
    private const int TransportLevelErrorNumber = 233;
    private const int TransportLevelErrorNumber2 = 234;

    /// <summary>
    ///     Initializes a new instance of the PriceUpdateService class.
    /// </summary>
    /// <param name="csvParser">The CSV parser service.</param>
    /// <param name="logger">The logger instance (optional - uses NullLogger if null).</param>
    public PriceUpdateService(ICsvParser csvParser, ILogger<PriceUpdateService>? logger)
    {
        _csvParser = csvParser ?? throw new ArgumentNullException(nameof(csvParser));
        _logger = logger ?? NullLogger<PriceUpdateService>.Instance;
    }

    /// <inheritdoc />
    public async Task<ExecutionSummary> ExecuteUpdateAsync(
        PriceUpdateConfiguration configuration,
        string csvFilePath,
        IProgress<ExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(csvFilePath);

        var startTime = DateTime.Now;
        var summary = new ExecutionSummary(
            totalRows: 0,
            updatedCount: 0,
            skippedCount: 0,
            errorCount: 0,
            startTime: startTime,
            endTime: startTime);

        try
        {
            // Validate configuration
            var validationResult = ValidateConfiguration(configuration);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    summary.AddError(new ExecutionError(0, string.Empty, error, "ValidationException"));
                }

                return summary.WithEndTime(DateTime.Now);
            }

            // Get total row count for progress reporting
            var totalRows = await CountCsvRowsAsync(csvFilePath, cancellationToken);
            summary = new ExecutionSummary(
                totalRows,
                updatedCount: 0,
                skippedCount: 0,
                errorCount: 0,
                startTime: startTime,
                endTime: startTime);

            _logger.LogInformation("Starting price update: {TotalRows} rows from {FilePath}", totalRows, csvFilePath);

            // Process CSV rows in batches
            var batchSize = configuration.BatchSize;
            var currentRow = 0;
            var updatedCount = 0;
            var skippedCount = 0;
            var errorCount = 0;
            var allResults = new List<UpdateResult>();

            await foreach (var batch in ParseInBatchesAsync(csvFilePath, batchSize, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batchResult = await ProcessBatchWithRetryAsync(
                    configuration,
                    batch,
                    cancellationToken);

                updatedCount += batchResult.UpdatedCount;
                skippedCount += batchResult.SkippedCount;
                errorCount += batchResult.ErrorCount;
                allResults.AddRange(batchResult.Results);
                currentRow += batch.Length;

                // Report progress
                var progressMessage = $"Processed batch: {batchResult.UpdatedCount} updated, {batchResult.SkippedCount} skipped, {batchResult.ErrorCount} errors";
                progress?.Report(new ExecutionProgress(currentRow, totalRows, progressMessage));

                _logger.LogDebug("Batch progress: {CurrentRow}/{TotalRows}", currentRow, totalRows);
            }

            summary = new ExecutionSummary(
                totalRows,
                updatedCount,
                skippedCount,
                errorCount,
                startTime,
                DateTime.Now);

            foreach (var result in allResults)
            {
                summary.AddResult(result);
            }

            _logger.LogInformation(
                "Price update completed: {UpdatedCount} updated, {SkippedCount} skipped, {ErrorCount} errors in {Duration:F2}s",
                updatedCount, skippedCount, errorCount, summary.DurationSeconds);

            return summary;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Price update operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Price update failed with unexpected error");
            summary.AddError(new ExecutionError(0, string.Empty, ex.Message, ex.GetType().Name));
            return summary.WithEndTime(DateTime.Now);
        }
    }

    /// <inheritdoc />
    public ValidationResult ValidateConfiguration(PriceUpdateConfiguration configuration)
    {
        var errors = new List<string>();

        if (configuration == null)
        {
            errors.Add("Configuration cannot be null.");
            return ValidationResult.Failure(errors);
        }

        if (configuration.SqlConnection == null)
        {
            errors.Add("SQL connection configuration is required.");
        }
        else
        {
            var connValidation = configuration.SqlConnection.Validate();
            if (!connValidation.IsValid)
            {
                errors.AddRange(connValidation.Errors);
            }
        }

        if (configuration.ColumnMapping == null)
        {
            errors.Add("Column mapping configuration is required.");
        }
        else if (!configuration.ColumnMapping.IsValid())
        {
            errors.Add("Column mapping is incomplete. All mapping fields are required.");
        }

        if (configuration.BatchSize <= 0)
        {
            errors.Add("Batch size must be greater than zero.");
        }

        if (configuration.CommandTimeout <= 0)
        {
            errors.Add("Command timeout must be greater than zero.");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }

    /// <inheritdoc />
    public async Task<TableMappingInfo?> GetTableSchemaAsync(
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
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to get table schema for {Schema}.{Table}", schemaName, tableName);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<int> CountCsvRowsAsync(string csvFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvFilePath);

        var count = 0;
        await foreach (var _ in _csvParser.ParseAsync(csvFilePath, cancellationToken))
        {
            count++;
        }

        return count;
    }

    /// <summary>
    ///     Parses CSV file in batches asynchronously.
    /// </summary>
    private async IAsyncEnumerable<CsvRow[]> ParseInBatchesAsync(
        string csvFilePath,
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var batch in _csvParser.ParseInBatchesAsync(csvFilePath, batchSize, cancellationToken))
        {
            yield return batch;
        }
    }

    /// <summary>
    ///     Processes a batch of CSV records with retry logic for transient failures.
    /// </summary>
    private async Task<BatchResult> ProcessBatchWithRetryAsync(
        PriceUpdateConfiguration configuration,
        CsvRow[] batch,
        CancellationToken cancellationToken)
    {
        var maxRetries = configuration.MaxRetryAttempts;
        var retryDelayMs = configuration.RetryDelayMs;
        var lastException = default(Exception);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await ProcessBatchAsync(configuration, batch, cancellationToken);
            }
            catch (SqlException ex) when (IsTransientError(ex) && attempt < maxRetries)
            {
                lastException = ex;
                _logger.LogWarning(
                    "Transient error on attempt {Attempt}/{MaxRetries}: {Error}. Retrying in {Delay}ms...",
                    attempt, maxRetries, ex.Message, retryDelayMs * attempt);

                await Task.Delay(retryDelayMs * attempt, cancellationToken);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error processing batch: {Error}", ex.Message);
                throw;
            }
        }

        throw lastException ?? new InvalidOperationException("Batch processing failed after all retry attempts");
    }

    /// <summary>
    ///     Processes a single batch of CSV records within a transaction.
    /// </summary>
    private async Task<BatchResult> ProcessBatchAsync(
        PriceUpdateConfiguration configuration,
        CsvRow[] batch,
        CancellationToken cancellationToken)
    {
        var updatedCount = 0;
        var skippedCount = 0;
        var errorCount = 0;
        var results = new List<UpdateResult>();

        var connectionString = configuration.SqlConnection!.BuildConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var csvRow in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var record = CsvRecord.FromCsvRow(csvRow, configuration.ColumnMapping!);

                if (!record.IsValid)
                {
                    results.Add(new UpdateResult(record.RowNumber, record.SearchTerm, IMOS.PriceUpdater.Models.UpdateStatus.Error, "Invalid CSV data"));
                    errorCount++;
                    continue;
                }

                var result = await UpdateSingleRowAsync(
                    connection,
                    transaction,
                    configuration,
                    record,
                    cancellationToken);

                results.Add(result);

                switch (result.Status)
                {
                    case IMOS.PriceUpdater.Models.UpdateStatus.Success:
                        updatedCount++;
                        break;
                    case IMOS.PriceUpdater.Models.UpdateStatus.Skipped:
                        skippedCount++;
                        break;
                    case IMOS.PriceUpdater.Models.UpdateStatus.Error:
                        errorCount++;
                        break;
                }
            }

            await transaction.CommitAsync(cancellationToken);

            return new BatchResult(updatedCount, skippedCount, errorCount, results);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    ///     Updates a single row in the database.
    /// </summary>
    private async Task<UpdateResult> UpdateSingleRowAsync(
        SqlConnection connection,
        IDbTransaction transaction,
        PriceUpdateConfiguration configuration,
        CsvRecord record,
        CancellationToken cancellationToken)
    {
        try
        {
            var mapping = configuration.ColumnMapping!;

            var updateQuery = $@"
                UPDATE [{mapping.SqlTable}]
                SET [{mapping.SqlPriceColumn}] = @Price
                WHERE [{mapping.SqlSearchColumn}] = @SearchValue";

            await using var command = new SqlCommand(updateQuery, (SqlConnection)connection, (SqlTransaction)transaction);
            command.CommandTimeout = configuration.CommandTimeout;
            command.Parameters.AddWithValue("@Price", record.Price);
            command.Parameters.AddWithValue("@SearchValue", record.SearchTerm);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                _logger.LogDebug("Row not found: {SearchValue} at line {LineNumber}", record.SearchTerm, record.RowNumber);
                return new UpdateResult(record.RowNumber, record.SearchTerm, IMOS.PriceUpdater.Models.UpdateStatus.Skipped);
            }

            return new UpdateResult(record.RowNumber, record.SearchTerm, IMOS.PriceUpdater.Models.UpdateStatus.Success);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Error updating row {RowNumber}: {Error}", record.RowNumber, ex.Message);
            return new UpdateResult(record.RowNumber, record.SearchTerm, IMOS.PriceUpdater.Models.UpdateStatus.Error, ex.Message);
        }
    }

    /// <summary>
    ///     Determines if a SQL exception represents a transient error that can be retried.
    /// </summary>
    private static bool IsTransientError(SqlException ex)
    {
        return ex.Number switch
        {
            ServerNotFoundErrorNumber => true,
            TimeoutErrorNumber => true,
            TransportLevelErrorNumber => true,
            TransportLevelErrorNumber2 => true,
            _ => false
        };
    }

    /// <summary>
    ///     Represents the result of processing a single batch.
    /// </summary>
    private sealed record BatchResult(int UpdatedCount, int SkippedCount, int ErrorCount, IReadOnlyList<UpdateResult> Results);
}

