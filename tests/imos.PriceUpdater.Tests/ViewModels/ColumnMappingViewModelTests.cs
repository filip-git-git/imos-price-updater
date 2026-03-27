using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.ViewModels;

namespace IMOS.PriceUpdater.Tests.ViewModels;

public class ColumnMappingViewModelTests
{
    [Fact]
    public void ColumnMappingViewModel_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var viewModel = new ColumnMappingViewModel();

        // Assert
        Assert.Null(viewModel.CsvFilePath);
        Assert.Empty(viewModel.CsvHeaders);
        Assert.Empty(viewModel.PreviewRows);
        Assert.Equal(string.Empty, viewModel.SelectedTable);
        Assert.Null(viewModel.SelectedSearchColumn);
        Assert.Null(viewModel.SelectedPriceColumn);
        Assert.False(viewModel.IsLoadingTables);
        Assert.False(viewModel.HasFile);
        Assert.False(viewModel.IsValidMapping);
    }

    [Fact]
    public void ColumnMappingViewModel_HasFile_WhenCsvFilePathSet()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel();

        // Act - set via property
        viewModel.CsvFilePath = "test.csv";

        // Assert
        Assert.True(viewModel.HasFile);
    }

    [Fact]
    public void ColumnMappingViewModel_HasValidMapping_WhenBothColumnsSelected()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel
        {
            SelectedSearchColumn = "MaterialCode",
            SelectedPriceColumn = "Price"
        };

        // Assert
        Assert.True(viewModel.HasValidMapping);
    }

    [Fact]
    public void ColumnMappingViewModel_HasValidMapping_False_WhenOnlySearchColumnSelected()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel
        {
            SelectedSearchColumn = "MaterialCode",
            SelectedPriceColumn = null
        };

        // Assert
        Assert.False(viewModel.HasValidMapping);
    }

    [Fact]
    public void ColumnMappingViewModel_HasValidMapping_False_WhenOnlyPriceColumnSelected()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel
        {
            SelectedSearchColumn = null,
            SelectedPriceColumn = "Price"
        };

        // Assert
        Assert.False(viewModel.HasValidMapping);
    }

    [Fact]
    public void ColumnMappingViewModel_HasSelectedTable_WhenTableNameSet()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel
        {
            SelectedTable = "dbo.Materials"
        };

        // Assert
        Assert.True(viewModel.HasSelectedTable);
    }

    [Fact]
    public void ColumnMappingViewModel_CanValidateMapping_WhenAllRequiredFieldsSet()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel
        {
            CsvFilePath = "test.csv",
            SelectedTable = "dbo.Materials",
            SelectedSearchColumn = "MaterialCode",
            SelectedPriceColumn = "Price"
        };

        // Assert
        Assert.True(viewModel.CanValidateMapping(null));
    }

    [Fact]
    public void ColumnMappingViewModel_CannotValidateMapping_WhenNoCsvFile()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel
        {
            CsvFilePath = null,
            SelectedTable = "dbo.Materials",
            SelectedSearchColumn = "MaterialCode",
            SelectedPriceColumn = "Price"
        };

        // Assert
        Assert.False(viewModel.CanValidateMapping(null));
    }

    [Fact]
    public void ColumnMappingViewModel_CannotValidateMapping_WhenNoTableSelected()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel
        {
            CsvFilePath = "test.csv",
            SelectedTable = "",
            SelectedSearchColumn = "MaterialCode",
            SelectedPriceColumn = "Price"
        };

        // Assert
        Assert.False(viewModel.CanValidateMapping(null));
    }

    [Fact]
    public void ColumnMappingViewModel_ClearMapping_ResetsAllProperties()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel
        {
            CsvFilePath = "test.csv",
            SelectedTable = "dbo.Materials",
            SelectedSearchColumn = "MaterialCode",
            SelectedPriceColumn = "Price"
        };

        // Act
        viewModel.ClearMapping();

        // Assert
        Assert.Null(viewModel.CsvFilePath);
        Assert.Empty(viewModel.CsvHeaders);
        Assert.Empty(viewModel.PreviewRows);
        Assert.Equal(string.Empty, viewModel.SelectedTable);
        Assert.Null(viewModel.SelectedSearchColumn);
        Assert.Null(viewModel.SelectedPriceColumn);
        Assert.False(viewModel.HasFile);
        Assert.False(viewModel.IsValidMapping);
        Assert.Equal(string.Empty, viewModel.ValidationMessage);
        Assert.Null(viewModel.LastValidationResult);
    }

    [Fact]
    public void ColumnMappingViewModel_CanLoadTables_WhenHasConnectionInfo()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel
        {
            ConnectionInfo = new SqlConnectionInfo
            {
                Server = "localhost",
                Database = "TestDb"
            }
        };

        // Assert
        Assert.True(viewModel.LoadTablesCommand.CanExecute(null));
    }

    [Fact]
    public void ColumnMappingViewModel_CannotLoadTables_WhenNoConnectionInfo()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel
        {
            ConnectionInfo = null
        };

        // Assert
        Assert.False(viewModel.LoadTablesCommand.CanExecute(null));
    }

    [Fact]
    public void ColumnMappingViewModel_CanExecuteSelectCsvFile_AlwaysReturnsTrue()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel();

        // Assert
        Assert.True(viewModel.SelectCsvFileCommand.CanExecute(null));
    }

    [Fact]
    public void ColumnMappingViewModel_HasFile_WhenFilePathIsSet()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel();

        // Act
        viewModel.CsvFilePath = "test.csv";

        // Assert
        Assert.True(viewModel.HasFile);
    }

    [Fact]
    public void ColumnMappingViewModel_HasFile_False_WhenFilePathIsNull()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel
        {
            CsvFilePath = "test.csv"
        };

        // Act - clear via ClearMapping
        viewModel.ClearMapping();

        // Assert
        Assert.False(viewModel.HasFile);
    }

    [Fact]
    public void ColumnMappingViewModel_ConnectionInfo_CanBeSet()
    {
        // Arrange
        var viewModel = new ColumnMappingViewModel();
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb"
        };

        // Act
        viewModel.ConnectionInfo = connectionInfo;

        // Assert
        Assert.Same(connectionInfo, viewModel.ConnectionInfo);
    }
}

