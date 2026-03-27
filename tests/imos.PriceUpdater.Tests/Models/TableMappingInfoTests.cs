using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Tests.Models;

public class TableMappingInfoTests
{
    [Fact]
    public void TableMappingInfo_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var tableMapping = new TableMappingInfo();

        // Assert
        Assert.Equal(string.Empty, tableMapping.TableName);
        Assert.Equal("dbo", tableMapping.SchemaName);
        Assert.Equal("dbo.", tableMapping.FullyQualifiedName);
        Assert.Empty(tableMapping.Columns);
    }

    [Fact]
    public void TableMappingInfo_FullyQualifiedName_ReturnsCorrectFormat()
    {
        // Arrange
        var tableMapping = new TableMappingInfo
        {
            SchemaName = "sales",
            TableName = "Products"
        };

        // Act & Assert
        Assert.Equal("sales.Products", tableMapping.FullyQualifiedName);
    }

    [Fact]
    public void TableMappingInfo_IsValid_WithValidData_ReturnsTrue()
    {
        // Arrange
        var tableMapping = new TableMappingInfo
        {
            SchemaName = "dbo",
            TableName = "Products",
            Columns = new List<ColumnInfo>
            {
                new ColumnInfo { Name = "Id", DataType = "int" }
            }
        };

        // Act & Assert
        Assert.True(tableMapping.IsValid());
    }

    [Fact]
    public void TableMappingInfo_IsValid_WithEmptyTableName_ReturnsFalse()
    {
        // Arrange
        var tableMapping = new TableMappingInfo
        {
            SchemaName = "dbo",
            TableName = "",
            Columns = new List<ColumnInfo>
            {
                new ColumnInfo { Name = "Id", DataType = "int" }
            }
        };

        // Act & Assert
        Assert.False(tableMapping.IsValid());
    }

    [Fact]
    public void TableMappingInfo_IsValid_WithEmptyColumns_ReturnsFalse()
    {
        // Arrange
        var tableMapping = new TableMappingInfo
        {
            SchemaName = "dbo",
            TableName = "Products",
            Columns = new List<ColumnInfo>()
        };

        // Act & Assert
        Assert.False(tableMapping.IsValid());
    }

    [Fact]
    public void ColumnInfo_IsSearchable_Varchar_ReturnsTrue()
    {
        // Arrange
        var column = new ColumnInfo
        {
            Name = "Name",
            DataType = "nvarchar"
        };

        // Act & Assert
        Assert.True(column.IsSearchable);
    }

    [Fact]
    public void ColumnInfo_IsSearchable_Int_ReturnsTrue()
    {
        // Arrange
        var column = new ColumnInfo
        {
            Name = "Id",
            DataType = "int"
        };

        // Act & Assert
        Assert.True(column.IsSearchable);
    }

    [Fact]
    public void ColumnInfo_IsSearchable_Bit_ReturnsFalse()
    {
        // Arrange
        var column = new ColumnInfo
        {
            Name = "IsActive",
            DataType = "bit"
        };

        // Act & Assert
        Assert.False(column.IsSearchable);
    }

    [Fact]
    public void ColumnInfo_IsPriceType_Decimal_ReturnsTrue()
    {
        // Arrange
        var column = new ColumnInfo
        {
            Name = "Price",
            DataType = "decimal(18,2)"
        };

        // Act & Assert
        Assert.True(column.IsPriceType);
    }

    [Fact]
    public void ColumnInfo_IsPriceType_Money_ReturnsTrue()
    {
        // Arrange
        var column = new ColumnInfo
        {
            Name = "Price",
            DataType = "money"
        };

        // Act & Assert
        Assert.True(column.IsPriceType);
    }

    [Fact]
    public void ColumnInfo_IsPriceType_Int_ReturnsFalse()
    {
        // Arrange
        var column = new ColumnInfo
        {
            Name = "Id",
            DataType = "int"
        };

        // Act & Assert
        Assert.False(column.IsPriceType);
    }

    [Fact]
    public void ColumnMappingValidationResult_Success_ReturnsValidResult()
    {
        // Act
        var result = ColumnMappingValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.True(result.IsSearchColumnValid);
        Assert.True(result.IsPriceColumnValid);
    }

    [Fact]
    public void ColumnMappingValidationResult_Failure_ReturnsFailedResult()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var result = ColumnMappingValidationResult.Failure(errors, false, true);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.False(result.IsSearchColumnValid);
        Assert.True(result.IsPriceColumnValid);
    }
}

