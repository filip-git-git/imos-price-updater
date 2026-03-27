using IMOS.PriceUpdater.Models;
using IMOS.PriceUpdater.Services;
using Moq;

namespace IMOS.PriceUpdater.Tests.Services;

public class SqlConnectionServiceTests
{
    [Fact]
    public void ValidateConnectionInfo_ValidSqlAuthConfig_ReturnsSuccess()
    {
        // Arrange
        var service = new SqlConnectionService();
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.SqlAuthentication,
            Username = "sa",
            Password = "password"
        };

        // Act
        var result = service.ValidateConnectionInfo(connectionInfo);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateConnectionInfo_ValidWindowsAuthConfig_ReturnsSuccess()
    {
        // Arrange
        var service = new SqlConnectionService();
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.WindowsAuthentication
        };

        // Act
        var result = service.ValidateConnectionInfo(connectionInfo);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateConnectionInfo_EmptyServer_ReturnsFailure()
    {
        // Arrange
        var service = new SqlConnectionService();
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.WindowsAuthentication
        };

        // Act
        var result = service.ValidateConnectionInfo(connectionInfo);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Server name is required"));
    }

    [Fact]
    public void ValidateConnectionInfo_EmptyDatabase_ReturnsFailure()
    {
        // Arrange
        var service = new SqlConnectionService();
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "",
            AuthenticationMode = AuthenticationMode.WindowsAuthentication
        };

        // Act
        var result = service.ValidateConnectionInfo(connectionInfo);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Database name is required"));
    }

    [Fact]
    public void ValidateConnectionInfo_SqlAuthWithoutUsername_ReturnsFailure()
    {
        // Arrange
        var service = new SqlConnectionService();
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.SqlAuthentication,
            Username = ""
        };

        // Act
        var result = service.ValidateConnectionInfo(connectionInfo);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Username is required"));
    }

    [Fact]
    public void ValidateConnectionInfo_InvalidServerCharacters_ReturnsFailure()
    {
        // Arrange
        var service = new SqlConnectionService();
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "server|name",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.WindowsAuthentication
        };

        // Act
        var result = service.ValidateConnectionInfo(connectionInfo);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Server name format is invalid"));
    }

    [Fact]
    public void ValidateConnectionInfo_WhitespaceOnlyServer_ReturnsFailure()
    {
        // Arrange
        var service = new SqlConnectionService();
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "   ",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.WindowsAuthentication
        };

        // Act
        var result = service.ValidateConnectionInfo(connectionInfo);

        // Assert
        Assert.False(result.IsValid);
    }
}

