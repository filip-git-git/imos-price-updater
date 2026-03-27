using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Tests.Models;

public class SqlConnectionConfigTests
{
    [Fact]
    public void IsValid_AllPropertiesSet_ReturnsTrue()
    {
        // Arrange
        var config = new SqlConnectionConfig
        {
            Server = "localhost",
            Database = "IMOS",
            Username = "sa",
            PasswordEnvVar = "IMOS_SQL_PASSWORD"
        };

        // Act
        var result = config.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_ServerEmpty_ReturnsFalse()
    {
        // Arrange
        var config = new SqlConnectionConfig
        {
            Server = "",
            Database = "IMOS",
            Username = "sa",
            PasswordEnvVar = "IMOS_SQL_PASSWORD"
        };

        // Act
        var result = config.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_DatabaseEmpty_ReturnsFalse()
    {
        // Arrange
        var config = new SqlConnectionConfig
        {
            Server = "localhost",
            Database = "",
            Username = "sa",
            PasswordEnvVar = "IMOS_SQL_PASSWORD"
        };

        // Act
        var result = config.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_UsernameEmpty_ReturnsFalse()
    {
        // Arrange
        var config = new SqlConnectionConfig
        {
            Server = "localhost",
            Database = "IMOS",
            Username = "",
            PasswordEnvVar = "IMOS_SQL_PASSWORD"
        };

        // Act
        var result = config.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_PasswordEnvVarEmpty_ReturnsFalse()
    {
        // Arrange
        var config = new SqlConnectionConfig
        {
            Server = "localhost",
            Database = "IMOS",
            Username = "sa",
            PasswordEnvVar = ""
        };

        // Act
        var result = config.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_AllPropertiesWhitespace_ReturnsFalse()
    {
        // Arrange
        var config = new SqlConnectionConfig
        {
            Server = "  ",
            Database = "\t",
            Username = "\n",
            PasswordEnvVar = " "
        };

        // Act
        var result = config.IsValid();

        // Assert
        Assert.False(result);
    }
}

