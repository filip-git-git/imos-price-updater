using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Tests.Models;

public class ColumnMappingTests
{
    [Fact]
    public void IsValid_AllPropertiesSet_ReturnsTrue()
    {
        // Arrange
        var mapping = new ColumnMapping
        {
            SqlTable = "Materials",
            CsvSearchColumn = "MaterialNo",
            CsvPriceColumn = "Price",
            SqlSearchColumn = "MaterialID",
            SqlPriceColumn = "Price"
        };

        // Act
        var result = mapping.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_SqlTableEmpty_ReturnsFalse()
    {
        // Arrange
        var mapping = new ColumnMapping
        {
            SqlTable = "",
            CsvSearchColumn = "MaterialNo",
            CsvPriceColumn = "Price",
            SqlSearchColumn = "MaterialID",
            SqlPriceColumn = "Price"
        };

        // Act
        var result = mapping.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_CsvSearchColumnEmpty_ReturnsFalse()
    {
        // Arrange
        var mapping = new ColumnMapping
        {
            SqlTable = "Materials",
            CsvSearchColumn = "",
            CsvPriceColumn = "Price",
            SqlSearchColumn = "MaterialID",
            SqlPriceColumn = "Price"
        };

        // Act
        var result = mapping.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_CsvPriceColumnEmpty_ReturnsFalse()
    {
        // Arrange
        var mapping = new ColumnMapping
        {
            SqlTable = "Materials",
            CsvSearchColumn = "MaterialNo",
            CsvPriceColumn = "",
            SqlSearchColumn = "MaterialID",
            SqlPriceColumn = "Price"
        };

        // Act
        var result = mapping.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_SqlSearchColumnEmpty_ReturnsFalse()
    {
        // Arrange
        var mapping = new ColumnMapping
        {
            SqlTable = "Materials",
            CsvSearchColumn = "MaterialNo",
            CsvPriceColumn = "Price",
            SqlSearchColumn = "",
            SqlPriceColumn = "Price"
        };

        // Act
        var result = mapping.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_SqlPriceColumnEmpty_ReturnsFalse()
    {
        // Arrange
        var mapping = new ColumnMapping
        {
            SqlTable = "Materials",
            CsvSearchColumn = "MaterialNo",
            CsvPriceColumn = "Price",
            SqlSearchColumn = "MaterialID",
            SqlPriceColumn = ""
        };

        // Act
        var result = mapping.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_AllPropertiesWhitespace_ReturnsFalse()
    {
        // Arrange
        var mapping = new ColumnMapping
        {
            SqlTable = "   ",
            CsvSearchColumn = "  ",
            CsvPriceColumn = "\t",
            SqlSearchColumn = "\n",
            SqlPriceColumn = " "
        };

        // Act
        var result = mapping.IsValid();

        // Assert
        Assert.False(result);
    }
}

