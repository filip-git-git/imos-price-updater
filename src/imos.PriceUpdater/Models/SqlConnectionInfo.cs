using Microsoft.Data.SqlClient;

namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Represents the authentication mode for SQL Server connection.
/// </summary>
public enum AuthenticationMode
{
    /// <summary>
    ///     Windows Authentication using current user credentials.
    /// </summary>
    WindowsAuthentication,

    /// <summary>
    ///     SQL Server Authentication using username and password.
    /// </summary>
    SqlAuthentication
}

/// <summary>
///     Information for connecting to SQL Server, including server name, database,
///     authentication mode, and credentials.
/// </summary>
public sealed class SqlConnectionInfo
{
    /// <summary>
    ///     Gets or sets the SQL Server instance name or address.
    /// </summary>
    /// <remarks>
    ///     Accepts formats like:
    ///     - localhost
    ///     - 192.168.1.100
    ///     - server.domain.com
    ///     - server.domain.com,1433
    /// </remarks>
    public string Server { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the name of the database to connect to.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the authentication mode.
    /// </summary>
    public AuthenticationMode AuthenticationMode { get; set; } = AuthenticationMode.SqlAuthentication;

    /// <summary>
    ///     Gets or sets the username for SQL Authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the password for SQL Authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the connection timeout in seconds.
    /// </summary>
    /// <remarks>
    ///     Default is 10 seconds as per acceptance criteria.
    /// </remarks>
    public int ConnectionTimeout { get; set; } = 10;

    /// <summary>
    ///     Validates that all required connection properties are configured.
    /// </summary>
    /// <returns>A validation result indicating success or failure with error messages.</returns>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Server))
        {
            errors.Add("Server name is required.");
        }
        else if (!IsValidServerFormat(Server))
        {
            errors.Add("Server name format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(Database))
        {
            errors.Add("Database name is required.");
        }

        if (AuthenticationMode == AuthenticationMode.SqlAuthentication)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                errors.Add("Username is required for SQL Authentication.");
            }
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }

    /// <summary>
    ///     Checks if the server name has a valid format.
    /// </summary>
    /// <param name="server">The server name to validate.</param>
    /// <returns>True if the format is valid; otherwise, false.</returns>
    private static bool IsValidServerFormat(string server)
    {
        // Server name should not be empty or whitespace
        if (string.IsNullOrWhiteSpace(server))
        {
            return false;
        }

        // Server name should not contain invalid characters
        var invalidChars = new[] { '<', '>', '"', '|', '\0' };
        if (server.IndexOfAny(invalidChars) >= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Creates a connection string from this configuration.
    /// </summary>
    /// <returns>A SQL Server connection string.</returns>
    public string BuildConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = Server,
            InitialCatalog = Database,
            ConnectTimeout = ConnectionTimeout,
            TrustServerCertificate = true,
            Encrypt = false
        };

        switch (AuthenticationMode)
        {
            case AuthenticationMode.WindowsAuthentication:
                builder.IntegratedSecurity = true;
                break;
            case AuthenticationMode.SqlAuthentication:
                builder.IntegratedSecurity = false;
                builder.UserID = Username;
                builder.Password = Password;
                break;
        }

        return builder.ConnectionString;
    }
}

/// <summary>
///     Represents the result of a validation operation.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    ///     Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    ///     Gets the collection of error messages if validation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    private ValidationResult()
    {
    }

    /// <summary>
    ///     Creates a successful validation result.
    /// </summary>
    /// <returns>A successful validation result.</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    ///     Creates a failed validation result.
    /// </summary>
    /// <param name="errors">The collection of error messages.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(IEnumerable<string> errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors.ToList().AsReadOnly()
        };
    }
}

