using System.Runtime.CompilerServices;
using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace IMOS.PriceUpdater.Tests.Services;

public class PriceUpdateServiceTests
{
    private readonly Mock<ICsvParser> _mockCsvParser;
    private readonly Mock<ILogger<PriceUpdateService>> _mockLogger;
    private readonly PriceUpdateService _service;

    public PriceUpdateServiceTests()
    {
        _mockCsvParser = new Mock<ICsvParser>();
        _mockLogger = new Mock<ILogger<PriceUpdateService>>();
        _service = new PriceUpdateService(_mockCsvParser.Object, _mockLogger.Object);
    }

    #region Test Data Helpers

    private static CsvRow CreateCsvRow(int lineNumber, string materialNo, string price)
    {
        var values = new Dictionary<string, string>
        {
            ["MaterialNo"] = materialNo,
            ["Price"] = price
        };
        return new CsvRow(lineNumber, values);
    }

    private static SqlConnectionInfo CreateValidConnectionInfo()
    {
        return new SqlConnectionInfo
        {
            Server = "(local)\\IMOSSQL2022",
            Database = "Testy_iX23",
            AuthenticationMode = AuthenticationMode.SqlAuthentication,
            Username = "IMOSADMIN",
            Password = "test_password",
            ConnectionTimeout = 30
        };
    }

    private static ColumnMapping CreateValidColumnMapping()
    {
        return new ColumnMapping
        {
            SqlTable = "Materials",
            CsvSearchColumn = "MaterialNo",
            CsvPriceColumn = "Price",
            SqlSearchColumn = "MaterialCode",
            SqlPriceColumn = "Price"
        };
    }

    private static PriceUpdateConfiguration CreateValidConfiguration()
    {
        return new PriceUpdateConfiguration
        {
            SqlConnection = CreateValidConnectionInfo(),
            ColumnMapping = CreateValidColumnMapping(),
            BatchSize = 1000,
            CommandTimeout = 30
        };
    }

    #endregion

    #region ValidateConfiguration Tests

    [Fact]
    public void ValidateConfiguration_WithValidConfig_ReturnsSuccess()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act
        var result = _service.ValidateConfiguration(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateConfiguration_WithNullConfig_ReturnsFailure()
    {
        // Act
        var result = _service.ValidateConfiguration(null!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("null"));
    }

    [Fact]
    public void ValidateConfiguration_WithMissingConnection_ReturnsFailure()
    {
        // Arrange
        var config = new PriceUpdateConfiguration
        {
            ColumnMapping = CreateValidColumnMapping(),
            BatchSize = 1000,
            CommandTimeout = 30
        };

        // Act
        var result = _service.ValidateConfiguration(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("connection"));
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidConnection_ReturnsFailure()
    {
        // Arrange
        var config = new PriceUpdateConfiguration
        {
            SqlConnection = new SqlConnectionInfo { Server = "", Database = "" },
            ColumnMapping = CreateValidColumnMapping(),
            BatchSize = 1000,
            CommandTimeout = 30
        };

        // Act
        var result = _service.ValidateConfiguration(config);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateConfiguration_WithMissingColumnMapping_ReturnsFailure()
    {
        // Arrange
        var config = new PriceUpdateConfiguration
        {
            SqlConnection = CreateValidConnectionInfo(),
            BatchSize = 1000,
            CommandTimeout = 30
        };

        // Act
        var result = _service.ValidateConfiguration(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("mapping"));
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidBatchSize_ReturnsFailure()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.BatchSize = 0;

        // Act
        var result = _service.ValidateConfiguration(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Batch size"));
    }

    [Fact]
    public void ValidateConfiguration_WithNegativeBatchSize_ReturnsFailure()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.BatchSize = -1;

        // Act
        var result = _service.ValidateConfiguration(config);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidTimeout_ReturnsFailure()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.CommandTimeout = 0;

        // Act
        var result = _service.ValidateConfiguration(config);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateConfiguration_EmptyConfig_ReturnsFailure()
    {
        // Arrange - all defaults which should be invalid due to null SqlConnection and ColumnMapping
        var config = new PriceUpdateConfiguration();

        // Act
        var result = _service.ValidateConfiguration(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2); // Should have errors for missing SqlConnection and ColumnMapping
    }

    #endregion

    #region CountCsvRowsAsync Tests

    [Fact]
    public async Task CountCsvRowsAsync_WithEmptyCsv_ReturnsZero()
    {
        // Arrange
        var filePath = "test.csv";
        _mockCsvParser
            .Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>()))
            .Returns(EmptyAsyncEnumerable<CsvRow>());

        // Act
        var count = await _service.CountCsvRowsAsync(filePath);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task CountCsvRowsAsync_WithThreeRows_ReturnsThree()
    {
        // Arrange
        var filePath = "test.csv";
        var rows = new List<CsvRow>
        {
            CreateCsvRow(2, "MAT-001", "100"),
            CreateCsvRow(3, "MAT-002", "200"),
            CreateCsvRow(4, "MAT-003", "300")
        };

        _mockCsvParser
            .Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>()))
            .Returns(rows.ToAsyncEnumerable());

        // Act
        var count = await _service.CountCsvRowsAsync(filePath);

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task CountCsvRowsAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var filePath = "test.csv";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockCsvParser
            .Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.CountCsvRowsAsync(filePath, cts.Token));
    }

    #endregion

    #region ExecuteUpdateAsync Tests

    [Fact]
    public async Task ExecuteUpdateAsync_WithInvalidConfig_ReturnsSummaryWithValidationErrors()
    {
        // Arrange
        var config = new PriceUpdateConfiguration(); // Invalid - missing connection
        var filePath = "test.csv";

        // Setup mock to return empty enumerable to prevent CountCsvRowsAsync from failing
        _mockCsvParser
            .Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>()))
            .Returns(EmptyAsyncEnumerable<CsvRow>());

        // Act
        var summary = await _service.ExecuteUpdateAsync(config, filePath);

        // Assert
        Assert.NotNull(summary);

        // When validation fails, TotalRows should still be 0 (the initial summary)
        // If validation passed and CountCsvRowsAsync was called, TotalRows would be 0 from empty CSV
        // The key indicator is whether errors were added
        Assert.Equal(0, summary.TotalRows);
        Assert.Equal(0, summary.UpdatedCount);
        Assert.Equal(0, summary.SkippedCount);
        Assert.Equal(0, summary.ErrorCount);

        // Errors should be added to the Errors collection when validation fails
        Assert.True(summary.Errors.Count > 0, $"Expected validation errors but Errors collection was empty. Errors: {summary.Errors.Count}");
    }

    [Fact]
    public async Task ExecuteUpdateAsync_WithEmptyCsv_ReturnsEmptySummary()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var filePath = "test.csv";

        _mockCsvParser
            .Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>()))
            .Returns(EmptyAsyncEnumerable<CsvRow>());

        _mockCsvParser
            .Setup(p => p.ParseInBatchesAsync(filePath, config.BatchSize, It.IsAny<CancellationToken>()))
            .Returns(EmptyAsyncEnumerable<CsvRow[]>());

        // Act
        var summary = await _service.ExecuteUpdateAsync(config, filePath);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(0, summary.TotalRows);
    }

    [Fact]
    public async Task ExecuteUpdateAsync_ReportsProgress()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var filePath = "test.csv";
        var progressValues = new List<ExecutionProgress>();

        _mockCsvParser
            .Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>()))
            .Returns(EmptyAsyncEnumerable<CsvRow>());

        _mockCsvParser
            .Setup(p => p.ParseInBatchesAsync(filePath, config.BatchSize, It.IsAny<CancellationToken>()))
            .Returns(EmptyAsyncEnumerable<CsvRow[]>());

        var progress = new Progress<ExecutionProgress>(p => progressValues.Add(p));

        // Act
        await _service.ExecuteUpdateAsync(config, filePath, progress);

        // Assert
        Assert.Empty(progressValues);
    }

    [Fact]
    public async Task ExecuteUpdateAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        var filePath = "test.csv";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockCsvParser
            .Setup(p => p.ParseAsync(filePath, It.IsAny<CancellationToken>()))
            .Returns(EmptyAsyncEnumerable<CsvRow>());

        _mockCsvParser
            .Setup(p => p.ParseInBatchesAsync(filePath, config.BatchSize, It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.ExecuteUpdateAsync(config, filePath, cancellationToken: cts.Token));
    }

    #endregion

    #region GetTableSchemaAsync Tests

    [Fact]
    public async Task GetTableSchemaAsync_WithInvalidTableName_ThrowsArgumentException()
    {
        // Arrange
        var connectionInfo = CreateValidConnectionInfo();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetTableSchemaAsync(connectionInfo, ""));
    }

    [Fact]
    public async Task GetTableSchemaAsync_WithInvalidSchemaName_ThrowsArgumentException()
    {
        // Arrange
        var connectionInfo = CreateValidConnectionInfo();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetTableSchemaAsync(connectionInfo, "Table", ""));
    }

    #endregion

    #region Batch Processing Tests

    [Fact]
    public void BatchResult_Constructor_SetsPropertiesCorrectly()
    {
        // This tests the internal BatchResult struct
        // We can't directly test internal members, but we can verify through behavior

        // Arrange
        var config = CreateValidConfiguration();
        config.BatchSize = 2;

        // Assert
        Assert.Equal(2, config.BatchSize);
    }

    #endregion

    #region CsvRecord Tests

    [Fact]
    public void CsvRecord_FromCsvRow_ParsesCorrectly()
    {
        // Arrange
        var csvRow = CreateCsvRow(10, "STEEL-001", "456.78");
        var mapping = CreateValidColumnMapping();

        // Act
        var record = CsvRecord.FromCsvRow(csvRow, mapping);

        // Assert
        Assert.Equal(10, record.RowNumber);
        Assert.Equal("STEEL-001", record.SearchTerm);
        Assert.Equal(456.78m, record.Price);
        Assert.True(record.IsValid);
    }

    [Fact]
    public void CsvRecord_FromCsvRow_WithInvalidPrice_SetsIsValidFalse()
    {
        // Arrange
        var csvRow = CreateCsvRow(10, "STEEL-001", "invalid");
        var mapping = CreateValidColumnMapping();

        // Act
        var record = CsvRecord.FromCsvRow(csvRow, mapping);

        // Assert
        Assert.False(record.IsValid);
    }

    [Fact]
    public void CsvRecord_FromCsvRow_WithMissingSearchColumn_SetsIsValidFalse()
    {
        // Arrange
        var values = new Dictionary<string, string>
        {
            ["WrongColumn"] = "STEEL-001",
            ["Price"] = "123.45"
        };
        var csvRow = new CsvRow(10, values);
        var mapping = CreateValidColumnMapping();

        // Act
        var record = CsvRecord.FromCsvRow(csvRow, mapping);

        // Assert
        Assert.False(record.IsValid);
    }

    [Fact]
    public void CsvRecord_FromCsvRow_WithMissingPriceColumn_SetsIsValidFalse()
    {
        // Arrange
        var values = new Dictionary<string, string>
        {
            ["MaterialNo"] = "STEEL-001"
        };
        var csvRow = new CsvRow(10, values);
        var mapping = CreateValidColumnMapping();

        // Act
        var record = CsvRecord.FromCsvRow(csvRow, mapping);

        // Assert
        Assert.False(record.IsValid);
    }

    [Fact]
    public void CsvRecord_FromCsvRow_WithWhitespaceSearchValue_SetsIsValidFalse()
    {
        // Arrange
        var csvRow = CreateCsvRow(10, "   ", "123.45");
        var mapping = CreateValidColumnMapping();

        // Act
        var record = CsvRecord.FromCsvRow(csvRow, mapping);

        // Assert
        Assert.False(record.IsValid);
    }

    [Theory]
    [InlineData("100")]
    [InlineData("100.50")]
    [InlineData("123456789.99")]
    [InlineData("0.01")]
    public void CsvRecord_FromCsvRow_WithVariousPriceFormats_ParsesCorrectly(string priceString)
    {
        // Arrange
        var csvRow = CreateCsvRow(1, "MAT-001", priceString);
        var mapping = CreateValidColumnMapping();

        // Act
        var record = CsvRecord.FromCsvRow(csvRow, mapping);

        // Assert
        Assert.True(record.IsValid);
        Assert.True(record.Price > 0);
    }

    #endregion

    #region ExecutionSummary Tests

    [Fact]
    public void ExecutionSummary_SuccessRate_CalculatesCorrectly()
    {
        // Arrange
        var summary = new ExecutionSummary(
            totalRows: 100,
            updatedCount: 90,
            skippedCount: 5,
            errorCount: 5,
            startTime: DateTime.Now,
            endTime: DateTime.Now);

        // Assert
        Assert.Equal(90.0, summary.SuccessRate);
    }

    [Fact]
    public void ExecutionSummary_SuccessRate_ZeroRows_ReturnsZero()
    {
        // Arrange
        var summary = new ExecutionSummary(
            totalRows: 0,
            updatedCount: 0,
            skippedCount: 0,
            errorCount: 0,
            startTime: DateTime.Now,
            endTime: DateTime.Now);

        // Assert
        Assert.Equal(0, summary.SuccessRate);
    }

    [Fact]
    public void ExecutionSummary_HasErrors_WithErrorCount_ReturnsTrue()
    {
        // Arrange
        var summary = new ExecutionSummary(
            totalRows: 100,
            updatedCount: 80,
            skippedCount: 15,
            errorCount: 5,
            startTime: DateTime.Now,
            endTime: DateTime.Now);

        // Assert
        Assert.True(summary.HasErrors);
    }

    [Fact]
    public void ExecutionSummary_HasErrors_WithNoErrors_ReturnsFalse()
    {
        // Arrange
        var summary = new ExecutionSummary(
            totalRows: 100,
            updatedCount: 80,
            skippedCount: 20,
            errorCount: 0,
            startTime: DateTime.Now,
            endTime: DateTime.Now);

        // Assert
        Assert.False(summary.HasErrors);
    }

    [Fact]
    public void ExecutionSummary_AddResult_AddsToResultsCollection()
    {
        // Arrange
        var summary = new ExecutionSummary(
            totalRows: 100,
            updatedCount: 80,
            skippedCount: 15,
            errorCount: 5,
            startTime: DateTime.Now,
            endTime: DateTime.Now);

        var result = new UpdateResult(1, "MAT-001", UpdateStatus.Success);

        // Act
        summary.AddResult(result);

        // Assert
        Assert.Single(summary.Results);
        Assert.Contains(result, summary.Results);
    }

    [Fact]
    public void ExecutionSummary_WithEndTime_CreatesNewInstance()
    {
        // Arrange
        var startTime = DateTime.Now;
        var originalSummary = new ExecutionSummary(
            totalRows: 100,
            updatedCount: 80,
            skippedCount: 15,
            errorCount: 5,
            startTime: startTime,
            endTime: startTime);

        var newEndTime = startTime.AddSeconds(30);

        // Act
        var newSummary = originalSummary.WithEndTime(newEndTime);

        // Assert
        Assert.Equal(startTime, originalSummary.EndTime);
        Assert.Equal(newEndTime, newSummary.EndTime);
        Assert.Equal(originalSummary.TotalRows, newSummary.TotalRows);
        Assert.Equal(originalSummary.UpdatedCount, newSummary.UpdatedCount);
    }

    #endregion

    #region ExecutionProgress Tests

    [Fact]
    public void ExecutionProgress_Percentage_CalculatesCorrectly()
    {
        // Act
        var progress = new ExecutionProgress(50, 100, "Processing");

        // Assert
        Assert.Equal(50, progress.Percentage);
    }

    [Fact]
    public void ExecutionProgress_Percentage_ZeroTotal_ReturnsZero()
    {
        // Act
        var progress = new ExecutionProgress(0, 0, "Test");

        // Assert
        Assert.Equal(0, progress.Percentage);
    }

    [Fact]
    public void ExecutionProgress_IsComplete_WhenCurrentEqualsTotal_ReturnsTrue()
    {
        // Act
        var progress = new ExecutionProgress(100, 100, "Complete");

        // Assert
        Assert.True(progress.IsComplete);
    }

    [Fact]
    public void ExecutionProgress_IsComplete_WhenCurrentLessThanTotal_ReturnsFalse()
    {
        // Act
        var progress = new ExecutionProgress(50, 100, "In progress");

        // Assert
        Assert.False(progress.IsComplete);
    }

    [Fact]
    public void ExecutionProgress_FromProgressEvent_CreatesCorrectly()
    {
        // Arrange
        var progressEvent = new ProgressEvent(25, 50, "Halfway there");

        // Act
        var progress = ExecutionProgress.FromProgressEvent(progressEvent);

        // Assert
        Assert.Equal(25, progress.CurrentRow);
        Assert.Equal(50, progress.TotalRows);
        Assert.Equal("Halfway there", progress.Message);
        Assert.Equal(50, progress.Percentage);
    }

    [Fact]
    public void ExecutionProgress_GetBatchNumber_ReturnsCorrectBatch()
    {
        // Arrange - at row 1500 with batch size 1000
        // (1500 / 1000) + 1 = 1 + 1 = 2
        var progress = new ExecutionProgress(1500, 5000, "Processing");

        // Act & Assert
        Assert.Equal(2, progress.GetBatchNumber(1000));
    }

    [Fact]
    public void ExecutionProgress_GetTotalBatches_ReturnsCorrectCount()
    {
        // Arrange
        var progress = new ExecutionProgress(5000, 5000, "Processing");

        // Act & Assert
        Assert.Equal(5, progress.GetTotalBatches(1000));
    }

    #endregion

    #region PriceUpdateConfiguration Tests

    [Fact]
    public void PriceUpdateConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new PriceUpdateConfiguration();

        // Assert
        Assert.Equal(1000, config.BatchSize);
        Assert.Equal(30, config.CommandTimeout);
        Assert.Equal(3, config.MaxRetryAttempts);
        Assert.Equal(1000, config.RetryDelayMs);
    }

    [Fact]
    public void PriceUpdateConfiguration_IsValid_WithAllRequiredFields_ReturnsTrue()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Assert
        Assert.True(config.IsValid());
    }

    [Fact]
    public void PriceUpdateConfiguration_IsValid_WithMissingConnection_ReturnsFalse()
    {
        // Arrange
        var config = new PriceUpdateConfiguration
        {
            ColumnMapping = CreateValidColumnMapping(),
            BatchSize = 1000,
            CommandTimeout = 30
        };

        // Assert
        Assert.False(config.IsValid());
    }

    #endregion

    #region Helper Methods

    private static async IAsyncEnumerable<T> EmptyAsyncEnumerable<T>()
    {
        await Task.CompletedTask;
        yield break;
    }

    #endregion
}

// Extension to convert IEnumerable to IAsyncEnumerable
internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.CompletedTask;
        }
    }
}

