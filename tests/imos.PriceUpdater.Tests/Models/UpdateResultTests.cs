using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Tests.Models;

public class UpdateResultTests
{
    [Fact]
    public void UpdateResult_Success_CreatesCorrectly()
    {
        // Act
        var result = new UpdateResult(1, "STEEL-001", UpdateStatus.Success);

        // Assert
        Assert.Equal(1, result.CsvLineNumber);
        Assert.Equal("STEEL-001", result.SearchValue);
        Assert.Equal(UpdateStatus.Success, result.Status);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void UpdateResult_Skipped_CreatesCorrectly()
    {
        // Act
        var result = new UpdateResult(2, "UNKNOWN-001", UpdateStatus.Skipped);

        // Assert
        Assert.Equal(2, result.CsvLineNumber);
        Assert.Equal("UNKNOWN-001", result.SearchValue);
        Assert.Equal(UpdateStatus.Skipped, result.Status);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void UpdateResult_Error_CreatesWithMessage()
    {
        // Act
        var result = new UpdateResult(3, "STEEL-001", UpdateStatus.Error, "Connection failed");

        // Assert
        Assert.Equal(3, result.CsvLineNumber);
        Assert.Equal("STEEL-001", result.SearchValue);
        Assert.Equal(UpdateStatus.Error, result.Status);
        Assert.Equal("Connection failed", result.ErrorMessage);
    }

    [Fact]
    public void UpdateResult_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var result1 = new UpdateResult(1, "STEEL-001", UpdateStatus.Success);
        var result2 = new UpdateResult(1, "STEEL-001", UpdateStatus.Success);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void UpdateResult_DifferentLineNumbers_NotEqual()
    {
        // Arrange
        var result1 = new UpdateResult(1, "STEEL-001", UpdateStatus.Success);
        var result2 = new UpdateResult(2, "STEEL-001", UpdateStatus.Success);

        // Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void UpdateResult_DifferentStatuses_NotEqual()
    {
        // Arrange
        var result1 = new UpdateResult(1, "STEEL-001", UpdateStatus.Success);
        var result2 = new UpdateResult(1, "STEEL-001", UpdateStatus.Skipped);

        // Assert
        Assert.NotEqual(result1, result2);
    }
}

