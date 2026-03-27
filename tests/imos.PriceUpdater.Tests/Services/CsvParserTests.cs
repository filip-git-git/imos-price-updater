using System.Text;
using IMOS.PriceUpdater.Services;

namespace IMOS.PriceUpdater.Tests.Services;

public class CsvParserTests
{
    private readonly CsvParser _parser;

    public CsvParserTests()
    {
        _parser = new CsvParser();
    }

    [Fact]
    public async Task ParseAsync_ValidCsv_ReturnsCorrectRows()
    {
        // Arrange
        var filePath = Path.Combine(GetTestDataPath(), "sample_valid.csv");

        // Act
        var rows = new List<IMOS.PriceUpdater.Models.CsvRow>();
        await foreach (var row in _parser.ParseAsync(filePath))
        {
            rows.Add(row);
        }

        // Assert
        Assert.Equal(3, rows.Count);
        Assert.Equal(2, rows[0].LineNumber);
        Assert.Equal("STEEL-001", rows[0].GetValue("MaterialNo"));
        Assert.Equal("123.45", rows[0].GetValue("Price"));
    }

    [Fact]
    public async Task ParseAsync_SingleRowCsv_ReturnsSingleRow()
    {
        // Arrange
        var filePath = Path.Combine(GetTestDataPath(), "sample_single_row.csv");

        // Act
        var rows = new List<IMOS.PriceUpdater.Models.CsvRow>();
        await foreach (var row in _parser.ParseAsync(filePath))
        {
            rows.Add(row);
        }

        // Assert
        Assert.Single(rows);
        Assert.Equal(2, rows[0].LineNumber);
    }

    [Fact]
    public async Task ParseAsync_EmptyCsv_ReturnsNoRows()
    {
        // Arrange
        var filePath = Path.Combine(GetTestDataPath(), "sample_empty.csv");

        // Act
        var rows = new List<IMOS.PriceUpdater.Models.CsvRow>();
        await foreach (var row in _parser.ParseAsync(filePath))
        {
            rows.Add(row);
        }

        // Assert
        Assert.Empty(rows);
    }

    [Fact]
    public async Task ParseInBatchesAsync_ValidCsv_ReturnsCorrectBatches()
    {
        // Arrange
        var filePath = Path.Combine(GetTestDataPath(), "sample_valid.csv");

        // Act
        var batches = new List<IMOS.PriceUpdater.Models.CsvRow[]>();
        await foreach (var batch in _parser.ParseInBatchesAsync(filePath, 2))
        {
            batches.Add(batch);
        }

        // Assert
        Assert.Equal(2, batches.Count);
        Assert.Equal(2, batches[0].Length);
        Assert.Single(batches[1]);
    }

    [Fact]
    public void DetectEncoding_Utf8Bom_ReturnsUtf8()
    {
        // Arrange
        var filePath = Path.Combine(GetTestDataPath(), "sample_utf8_bom.csv");

        // Act
        var encoding = _parser.DetectEncoding(filePath);

        // Assert
        Assert.Equal(Encoding.UTF8, encoding);
    }

    [Fact]
    public async Task ValidateColumnsAsync_ValidColumns_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(GetTestDataPath(), "sample_valid.csv");
        var requiredColumns = new[] { "MaterialNo", "Description", "Price" };

        // Act
        var result = await _parser.ValidateColumnsAsync(filePath, requiredColumns);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateColumnsAsync_MissingColumn_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(GetTestDataPath(), "sample_missing_column.csv");
        var requiredColumns = new[] { "MaterialNo", "Description", "Price" };

        // Act
        var result = await _parser.ValidateColumnsAsync(filePath, requiredColumns);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateColumnsAsync_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(GetTestDataPath(), "sample_valid.csv");
        var requiredColumns = new[] { "materialno", "description", "price" };

        // Act
        var result = await _parser.ValidateColumnsAsync(filePath, requiredColumns);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateColumnsAsync_NullColumns_ThrowsArgumentNullException()
    {
        // Arrange
        var filePath = Path.Combine(GetTestDataPath(), "sample_valid.csv");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _parser.ValidateColumnsAsync(filePath, null!));
    }

    private static string GetTestDataPath()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(basePath, "TestData");
    }
}

