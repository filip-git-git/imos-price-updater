namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents the result of testing a database connection.
/// </summary>
public sealed class ConnectionTestResult
{
    /// <summary>
    ///     Gets a value indicating whether the connection was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    ///     Gets the server name that was connected to (if successful).
    /// </summary>
    public string? ServerName { get; private set; }

    /// <summary>
    ///     Gets the database name that was connected to (if successful).
    /// </summary>
    public string? DatabaseName { get; private set; }

    /// <summary>
    ///     Gets the error message if the connection failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    ///     Gets the error type for categorization.
    /// </summary>
    public ConnectionErrorType ErrorType { get; private set; }

    /// <summary>
    ///     Gets troubleshooting tips if the connection failed.
    /// </summary>
    public IReadOnlyList<string> TroubleshootingTips { get; private set; } = Array.Empty<string>();

    private ConnectionTestResult()
    {
    }

    /// <summary>
    ///     Creates a successful connection result.
    /// </summary>
    /// <param name="serverName">The server name.</param>
    /// <param name="databaseName">The database name.</param>
    /// <returns>A successful connection result.</returns>
    public static ConnectionTestResult Success(string serverName, string databaseName)
    {
        return new ConnectionTestResult
        {
            IsSuccess = true,
            ServerName = serverName,
            DatabaseName = databaseName
        };
    }

    /// <summary>
    ///     Creates a failed connection result due to timeout.
    /// </summary>
    /// <param name="serverName">The server name that timed out.</param>
    /// <param name="timeoutSeconds">The timeout in seconds.</param>
    /// <returns>A timeout connection result.</returns>
    public static ConnectionTestResult Timeout(string serverName, int timeoutSeconds)
    {
        return new ConnectionTestResult
        {
            IsSuccess = false,
            ServerName = serverName,
            ErrorMessage = $"Connection timed out after {timeoutSeconds} seconds.",
            ErrorType = ConnectionErrorType.Timeout,
            TroubleshootingTips = new List<string>
            {
                "Check if the server name is correct.",
                "Verify the SQL Server service is running.",
                "Check if a firewall is blocking the connection.",
                "Verify the server is network accessible.",
                $"Try increasing the timeout value beyond {timeoutSeconds} seconds."
            }
        };
    }

    /// <summary>
    ///     Creates a failed connection result due to invalid credentials.
    /// </summary>
    /// <param name="serverName">The server name.</param>
    /// <param name="databaseName">The database name.</param>
    /// <returns>An invalid credentials connection result.</returns>
    public static ConnectionTestResult InvalidCredentials(string serverName, string databaseName)
    {
        return new ConnectionTestResult
        {
            IsSuccess = false,
            ServerName = serverName,
            DatabaseName = databaseName,
            ErrorMessage = "Invalid username or password.",
            ErrorType = ConnectionErrorType.InvalidCredentials,
            TroubleshootingTips = new List<string>
            {
                "Verify the username is correct.",
                "Check if the password has been changed.",
                "Ensure the user has permission to access the database.",
                "Try using Windows Authentication instead."
            }
        };
    }

    /// <summary>
    ///     Creates a failed connection result due to server not found.
    /// </summary>
    /// <param name="serverName">The server name that was not found.</param>
    /// <returns>A server not found connection result.</returns>
    public static ConnectionTestResult ServerNotFound(string serverName)
    {
        return new ConnectionTestResult
        {
            IsSuccess = false,
            ServerName = serverName,
            ErrorMessage = $"Server '{serverName}' was not found or is not accessible.",
            ErrorType = ConnectionErrorType.ServerNotFound,
            TroubleshootingTips = new List<string>
            {
                "Verify the server name is correct.",
                "Check if the server is running.",
                "Ensure the server is network accessible.",
                "Try using the IP address instead of the server name.",
                "Check if SQL Server is installed on the target machine."
            }
        };
    }

    /// <summary>
    ///     Creates a failed connection result due to network error.
    /// </summary>
    /// <param name="serverName">The server name.</param>
    /// <returns>A network error connection result.</returns>
    public static ConnectionTestResult NetworkError(string serverName)
    {
        return new ConnectionTestResult
        {
            IsSuccess = false,
            ServerName = serverName,
            ErrorMessage = "A network-related error occurred while connecting to the server.",
            ErrorType = ConnectionErrorType.NetworkError,
            TroubleshootingTips = new List<string>
            {
                "Check your network connection.",
                "Verify the server is accessible from your machine.",
                "Check if a firewall is blocking the connection.",
                "Ensure the SQL Server Browser service is running.",
                "Verify TCP/IP protocol is enabled on the server."
            }
        };
    }

    /// <summary>
    ///     Creates a failed connection result due to database not found.
    /// </summary>
    /// <param name="serverName">The server name.</param>
    /// <param name="databaseName">The database name that was not found.</param>
    /// <returns>A database not found connection result.</returns>
    public static ConnectionTestResult DatabaseNotFound(string serverName, string databaseName)
    {
        return new ConnectionTestResult
        {
            IsSuccess = false,
            ServerName = serverName,
            DatabaseName = databaseName,
            ErrorMessage = $"Database '{databaseName}' was not found on the server.",
            ErrorType = ConnectionErrorType.DatabaseNotFound,
            TroubleshootingTips = new List<string>
            {
                "Verify the database name is correct.",
                "Check if the database exists on the server.",
                "Ensure the user has access to the database.",
                "Try listing available databases to confirm."
            }
        };
    }

    /// <summary>
    ///     Creates a failed connection result with a generic error.
    /// </summary>
    /// <param name="serverName">The server name.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A generic error connection result.</returns>
    public static ConnectionTestResult Error(string serverName, string errorMessage)
    {
        return new ConnectionTestResult
        {
            IsSuccess = false,
            ServerName = serverName,
            ErrorMessage = errorMessage,
            ErrorType = ConnectionErrorType.Other,
            TroubleshootingTips = new List<string>
            {
                "Review the error message for details.",
                "Try reconnecting with corrected settings.",
                "Contact your database administrator."
            }
        };
    }
}

/// <summary>
///     Categorizes the type of connection error.
/// </summary>
public enum ConnectionErrorType
{
    /// <summary>
    ///     No error (successful connection).
    /// </summary>
    None,

    /// <summary>
    ///     Connection timed out.
    /// </summary>
    Timeout,

    /// <summary>
    ///     Invalid username or password.
    /// </summary>
    InvalidCredentials,

    /// <summary>
    ///     Server was not found or not accessible.
    /// </summary>
    ServerNotFound,

    /// <summary>
    ///     Network-related error.
    /// </summary>
    NetworkError,

    /// <summary>
    ///     Database was not found on the server.
    /// </summary>
    DatabaseNotFound,

    /// <summary>
    ///     Other error.
    /// </summary>
    Other
}

