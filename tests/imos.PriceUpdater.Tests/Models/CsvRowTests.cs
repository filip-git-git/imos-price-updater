using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Tests.Models;

public class CsvRowTests
{
    [Fact]
    public void CsvRow_WithValidValues_InitializesCorrectly()
    {
        // Arrange
        var values = new Dictionary<string, string>
        {
            { "Material", "STEEL-001" },
            { "Price", "123.45" }
        };

        // Act
        var row = new CsvRow(1, values);

        // Assert
        Assert.Equal(1, row.LineNumber);
        Assert.Equal(2, row.Values.Count);
        Assert.Equal("STEEL-001", row.Values["Material"]);
        Assert.Equal("123.45", row.Values["Price"]);
    }

    [Fact]
    public void GetValue_ExistingColumn_ReturnsValue()
    {
        // Arrange
        var values = new Dictionary<string, string> { { "Material", "STEEL-001" } };
        var row = new CsvRow(1, values);

        // Act
        var result = row.GetValue("Material");

        // Assert
        Assert.Equal("STEEL-001", result);
    }

    [Fact]
    public void GetValue_NonExistingColumn_ReturnsNull()
    {
        // Arrange
        var values = new Dictionary<string, string> { { "Material", "STEEL-001" } };
        var row = new CsvRow(1, values);

        // Act
        var result = row.GetValue("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryGetValue_ExistingColumn_ReturnsTrueAndValue()
    {
        // Arrange
        var values = new Dictionary<string, string> { { "Material", "STEEL-001" } };
        var row = new CsvRow(1, values);

        // Act
        var success = row.TryGetValue("Material", out var value);

        // Assert
        Assert.True(success);
        Assert.Equal("STEEL-001", value);
    }

    [Fact]
    public void TryGetValue_NonExistingColumn_ReturnsFalse()
    {
        // Arrange
        var values = new Dictionary<string, string> { { "Material", "STEEL-001" } };
        var row = new CsvRow(1, values);

        // Act
        var success = row.TryGetValue("NonExistent", out var value);

        // Assert
        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void CsvRow_RecordEquality_SameReference_AreEqual()
    {
        // Arrange
        var values = new Dictionary<string, string> { { "Material", "STEEL-001" } };
        var row = new CsvRow(1, values);

        // Assert - Same instance should be equal
        Assert.Same(row, row);
    }

    [Fact]
    public void CsvRow_RecordEquality_DifferentLineNumbers_AreNotEqual()
    {
        // Arrange
        var values = new Dictionary<string, string> { { "Material", "STEEL-001" } };
        var row1 = new CsvRow(1, values);
        var row2 = new CsvRow(2, values);

        // Assert
        Assert.NotEqual(row1, row2);
    }
}

