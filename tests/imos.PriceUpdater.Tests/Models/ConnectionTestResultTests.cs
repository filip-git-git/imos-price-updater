using IMOS.PriceUpdater.Models;

namespace IMOS.PriceUpdater.Tests.Models;

public class ConnectionTestResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        // Act
        var result = ConnectionTestResult.Success("localhost", "TestDb");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("localhost", result.ServerName);
        Assert.Equal("TestDb", result.DatabaseName);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(ConnectionErrorType.None, result.ErrorType);
    }

    [Fact]
    public void Timeout_CreatesTimeoutResult()
    {
        // Act
        var result = ConnectionTestResult.Timeout("localhost", 10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("localhost", result.ServerName);
        Assert.Contains("10 seconds", result.ErrorMessage);
        Assert.Equal(ConnectionErrorType.Timeout, result.ErrorType);
        Assert.NotEmpty(result.TroubleshootingTips);
    }

    [Fact]
    public void InvalidCredentials_CreatesInvalidCredentialsResult()
    {
        // Act
        var result = ConnectionTestResult.InvalidCredentials("localhost", "TestDb");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("localhost", result.ServerName);
        Assert.Equal("TestDb", result.DatabaseName);
        Assert.Contains("Invalid username or password", result.ErrorMessage);
        Assert.Equal(ConnectionErrorType.InvalidCredentials, result.ErrorType);
        Assert.NotEmpty(result.TroubleshootingTips);
    }

    [Fact]
    public void ServerNotFound_CreatesServerNotFoundResult()
    {
        // Act
        var result = ConnectionTestResult.ServerNotFound("nonexistent");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("nonexistent", result.ServerName);
        Assert.Contains("not found", result.ErrorMessage);
        Assert.Equal(ConnectionErrorType.ServerNotFound, result.ErrorType);
        Assert.NotEmpty(result.TroubleshootingTips);
    }

    [Fact]
    public void NetworkError_CreatesNetworkErrorResult()
    {
        // Act
        var result = ConnectionTestResult.NetworkError("localhost");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("localhost", result.ServerName);
        Assert.Contains("network", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ConnectionErrorType.NetworkError, result.ErrorType);
        Assert.NotEmpty(result.TroubleshootingTips);
    }

    [Fact]
    public void DatabaseNotFound_CreatesDatabaseNotFoundResult()
    {
        // Act
        var result = ConnectionTestResult.DatabaseNotFound("localhost", "NonExistentDb");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("localhost", result.ServerName);
        Assert.Equal("NonExistentDb", result.DatabaseName);
        Assert.Contains("not found", result.ErrorMessage);
        Assert.Equal(ConnectionErrorType.DatabaseNotFound, result.ErrorType);
        Assert.NotEmpty(result.TroubleshootingTips);
    }

    [Fact]
    public void Error_CreatesGenericErrorResult()
    {
        // Act
        var result = ConnectionTestResult.Error("localhost", "Something went wrong");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("localhost", result.ServerName);
        Assert.Equal("Something went wrong", result.ErrorMessage);
        Assert.Equal(ConnectionErrorType.Other, result.ErrorType);
        Assert.NotEmpty(result.TroubleshootingTips);
    }
}

