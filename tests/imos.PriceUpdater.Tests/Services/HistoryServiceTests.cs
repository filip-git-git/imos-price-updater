using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace IMOS.PriceUpdater.Tests.Services;

public class HistoryServiceTests : IDisposable
{
    private readonly Mock<ILogger<HistoryService>> _mockLogger;
    private readonly HistoryService _service;
    private readonly string _testHistoryDirectory;

    public HistoryServiceTests()
    {
        _mockLogger = new Mock<ILogger<HistoryService>>();
        _service = new HistoryService(_mockLogger.Object);
        
        // Use reflection to get the history directory
        var type = typeof(HistoryService);
        var field = type.GetField("_historyDirectory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        _testHistoryDirectory = field?.GetValue(_service) as string ?? string.Empty;
    }

    public void Dispose()
    {
        // Clean up test files
        if (Directory.Exists(_testHistoryDirectory))
        {
            foreach (var file in Directory.GetFiles(_testHistoryDirectory, "*.json"))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    #region Test Data Helpers

    private static ExecutionHistory CreateTestHistory(
        Guid? id = null,
        string csvFileName = "test.csv",
        ExecutionOutcome outcome = ExecutionOutcome.Success)
    {
        return new ExecutionHistory
        {
            Id = id ?? Guid.NewGuid(),
            ExecutedAt = DateTime.UtcNow,
            CsvFileName = csvFileName,
            CsvFilePath = $"C:\\test\\{csvFileName}",
            ConfigFileName = "config.json",
            Outcome = outcome,
            TotalRows = 100,
            UpdatedCount = 90,
            SkippedCount = 5,
            ErrorCount = 5,
            DurationSeconds = 30
        };
    }

    private static ExecutionHistoryDetail CreateTestDetail(
        Guid executionId,
        string materialId = "MAT-001",
        ExecutionStatus status = ExecutionStatus.Updated)
    {
        return new ExecutionHistoryDetail
        {
            ExecutionId = executionId,
            MaterialId = materialId,
            SearchTerm = "Test Material",
            OldPrice = 100.00m,
            NewPrice = 110.00m,
            Status = status,
            SourceRowNumber = 2
        };
    }

    #endregion

    #region SaveExecutionAsync Tests

    [Fact]
    public async Task SaveExecutionAsync_WithValidHistory_SavesSuccessfully()
    {
        // Arrange
        var history = CreateTestHistory();

        // Act
        await _service.SaveExecutionAsync(history);

        // Assert
        var loaded = await _service.GetHistoryByIdAsync(history.Id);
        Assert.NotNull(loaded);
        Assert.Equal(history.CsvFileName, loaded.CsvFileName);
        Assert.Equal(history.Outcome, loaded.Outcome);
        Assert.Equal(history.TotalRows, loaded.TotalRows);
    }

    [Fact]
    public async Task SaveExecutionAsync_WithNullHistory_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.SaveExecutionAsync(null!));
    }

    #endregion

    #region GetHistoryAsync Tests

    [Fact]
    public async Task GetHistoryAsync_ReturnsAllHistorySortedByDate()
    {
        // Arrange
        var history1 = CreateTestHistory(outcome: ExecutionOutcome.Success);
        var history2 = CreateTestHistory(outcome: ExecutionOutcome.Failed);
        
        await _service.SaveExecutionAsync(history1);
        await Task.Delay(10); // Ensure different timestamps
        await _service.SaveExecutionAsync(history2);

        // Act
        var result = await _service.GetHistoryAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(history2.Id, result[0].Id); // Most recent first
        Assert.Equal(history1.Id, result[1].Id);
    }

    [Fact]
    public async Task GetHistoryAsync_WithOutcomeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var successHistory = CreateTestHistory(outcome: ExecutionOutcome.Success);
        var failedHistory = CreateTestHistory(outcome: ExecutionOutcome.Failed);
        
        await _service.SaveExecutionAsync(successHistory);
        await _service.SaveExecutionAsync(failedHistory);

        // Act
        var result = await _service.GetHistoryAsync(outcome: ExecutionOutcome.Failed);

        // Assert
        Assert.Single(result);
        Assert.Equal(ExecutionOutcome.Failed, result[0].Outcome);
    }

    [Fact]
    public async Task GetHistoryAsync_WithDateRange_FiltersCorrectly()
    {
        // Arrange
        var oldHistory = CreateTestHistory();
        oldHistory.ExecutedAt = DateTime.UtcNow.AddDays(-30);
        
        var recentHistory = CreateTestHistory();
        recentHistory.ExecutedAt = DateTime.UtcNow;

        await _service.SaveExecutionAsync(oldHistory);
        await _service.SaveExecutionAsync(recentHistory);

        // Act
        var result = await _service.GetHistoryAsync(
            fromDate: DateTime.UtcNow.AddDays(-7),
            toDate: DateTime.UtcNow.AddDays(1));

        // Assert
        Assert.Single(result);
        Assert.Equal(recentHistory.Id, result[0].Id);
    }

    #endregion

    #region GetHistoryByIdAsync Tests

    [Fact]
    public async Task GetHistoryByIdAsync_WithExistingId_ReturnsHistory()
    {
        // Arrange
        var history = CreateTestHistory();
        await _service.SaveExecutionAsync(history);

        // Act
        var result = await _service.GetHistoryByIdAsync(history.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(history.Id, result.Id);
    }

    [Fact]
    public async Task GetHistoryByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _service.GetHistoryByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteHistoryEntryAsync Tests

    [Fact]
    public async Task DeleteHistoryEntryAsync_WithExistingId_DeletesSuccessfully()
    {
        // Arrange
        var history = CreateTestHistory();
        await _service.SaveExecutionAsync(history);

        // Act
        await _service.DeleteHistoryEntryAsync(history.Id);

        // Assert
        var result = await _service.GetHistoryByIdAsync(history.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteHistoryEntryAsync_WithNonExistingId_DoesNotThrow()
    {
        // Act & Assert
        await _service.DeleteHistoryEntryAsync(Guid.NewGuid()); // Should not throw
    }

    #endregion

    #region SaveExecutionDetailsAsync Tests

    [Fact]
    public async Task SaveExecutionDetailsAsync_SavesDetailsCorrectly()
    {
        // Arrange
        var history = CreateTestHistory();
        await _service.SaveExecutionAsync(history);
        
        var details = new List<ExecutionHistoryDetail>
        {
            CreateTestDetail(history.Id, "MAT-001", ExecutionStatus.Updated),
            CreateTestDetail(history.Id, "MAT-002", ExecutionStatus.Skipped),
            CreateTestDetail(history.Id, "MAT-003", ExecutionStatus.Error)
        };

        // Act
        await _service.SaveExecutionDetailsAsync(history.Id, details);

        // Assert
        var loadedDetails = await _service.GetHistoryDetailsAsync(history.Id);
        Assert.Equal(3, loadedDetails.Count);
    }

    #endregion

    #region GetHistoryCountAsync Tests

    [Fact]
    public async Task GetHistoryCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var history1 = CreateTestHistory();
        var history2 = CreateTestHistory();
        
        await _service.SaveExecutionAsync(history1);
        await _service.SaveExecutionAsync(history2);

        // Act
        var count = await _service.GetHistoryCountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    #endregion

    #region CleanupOldHistoryAsync Tests

    [Fact]
    public async Task CleanupOldHistoryAsync_DeletesOldEntries()
    {
        // Arrange
        var histories = new List<ExecutionHistory>();
        for (int i = 0; i < 15; i++)
        {
            var history = CreateTestHistory();
            history.ExecutedAt = DateTime.UtcNow.AddDays(-i);
            histories.Add(history);
            await _service.SaveExecutionAsync(history);
        }

        // Act
        await _service.CleanupOldHistoryAsync(10);

        // Assert
        var count = await _service.GetHistoryCountAsync();
        Assert.Equal(10, count);
    }

    [Fact]
    public async Task CleanupOldHistoryAsync_WithInvalidKeepCount_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CleanupOldHistoryAsync(0));
    }

    #endregion
}
