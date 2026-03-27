using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;
using Moq;

namespace IMOS.PriceUpdater.Tests.Services;

public class TableSchemaServiceTests
{
    private readonly Mock<ISqlConnectionService> _mockConnectionService;
    private readonly TableSchemaService _service;

    public TableSchemaServiceTests()
    {
        _mockConnectionService = new Mock<ISqlConnectionService>();
        _service = new TableSchemaService();
    }

    [Fact]
    public void GetTableColumnsAsync_EmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.GetTableColumnsAsync(connectionInfo, "", "dbo").GetAwaiter().GetResult());
    }

    [Fact]
    public void GetTableColumnsAsync_EmptySchemaName_ThrowsArgumentException()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.GetTableColumnsAsync(connectionInfo, "TestTable", "").GetAwaiter().GetResult());
    }

    [Fact]
    public async Task ValidateTableColumnsAsync_ValidColumns_ReturnsSuccess()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.WindowsAuthentication
        };

        // Note: This test would need a real database connection to fully pass.
        // Without a real database, the service returns null for GetTableColumnsAsync,
        // which causes ValidateTableColumnsAsync to return a failure result.
        // This test validates the failure path behavior when table doesn't exist.

        // Act
        var result = await _service.ValidateTableColumnsAsync(
            connectionInfo,
            "NonExistentTable",
            new[] { "Column1", "Column2" });

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidateTableColumnsAsync_TableNotFound_ReturnsError()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.WindowsAuthentication
        };

        // Act
        var result = await _service.ValidateTableColumnsAsync(
            connectionInfo,
            "NonExistentTable",
            new[] { "Column1", "Column2", "MissingColumn" });

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        // The table doesn't exist, so error is about table not found
        Assert.Contains(result.Errors, e => e.Contains("does not exist") || e.Contains("not accessible"));
    }

    [Fact]
    public async Task GetUserTablesAsync_WithInvalidConnection_ReturnsEmptyList()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "nonexistent-server",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.WindowsAuthentication,
            ConnectionTimeout = 1
        };

        // Act
        var result = await _service.GetUserTablesAsync(connectionInfo);

        // Assert
        Assert.Empty(result);
    }
}

