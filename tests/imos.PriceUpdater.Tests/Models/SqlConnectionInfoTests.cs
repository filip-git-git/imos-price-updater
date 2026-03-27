using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Tests.Models;

public class SqlConnectionInfoTests
{
    [Fact]
    public void BuildConnectionString_SqlAuthentication_BuildsCorrectString()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.SqlAuthentication,
            Username = "sa",
            Password = "password123",
            ConnectionTimeout = 15
        };

        // Act
        var connectionString = connectionInfo.BuildConnectionString();

        // Assert
        Assert.Contains("localhost", connectionString);
        Assert.Contains("TestDb", connectionString);
        Assert.Contains("User ID=sa", connectionString);
        Assert.Contains("Password=password123", connectionString);
        Assert.Contains("Connect Timeout=15", connectionString);
        Assert.Contains("Integrated Security=False", connectionString);
    }

    [Fact]
    public void BuildConnectionString_WindowsAuthentication_BuildsCorrectString()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.WindowsAuthentication,
            ConnectionTimeout = 10
        };

        // Act
        var connectionString = connectionInfo.BuildConnectionString();

        // Assert
        Assert.Contains("localhost", connectionString);
        Assert.Contains("TestDb", connectionString);
        Assert.Contains("Integrated Security=True", connectionString);
    }

    [Fact]
    public void Validate_ValidSqlAuthConfig_ReturnsSuccess()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.SqlAuthentication,
            Username = "sa"
        };

        // Act
        var result = connectionInfo.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ValidWindowsAuthConfig_ReturnsSuccess()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.WindowsAuthentication
        };

        // Act
        var result = connectionInfo.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmptyServer_ReturnsFailure()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "",
            Database = "TestDb"
        };

        // Act
        var result = connectionInfo.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Server name is required"));
    }

    [Fact]
    public void Validate_InvalidServerCharacters_ReturnsFailure()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "server<name",
            Database = "TestDb"
        };

        // Act
        var result = connectionInfo.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Server name format is invalid"));
    }

    [Fact]
    public void Validate_SqlAuthWithoutUsername_ReturnsFailure()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = "TestDb",
            AuthenticationMode = AuthenticationMode.SqlAuthentication,
            Username = ""
        };

        // Act
        var result = connectionInfo.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Username is required"));
    }

    [Fact]
    public void Validate_EmptyDatabase_ReturnsFailure()
    {
        // Arrange
        var connectionInfo = new SqlConnectionInfo
        {
            Server = "localhost",
            Database = ""
        };

        // Act
        var result = connectionInfo.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Database name is required"));
    }
}

