namespace IMOS.PriceUpdater.Models;

/// <summary>
///     Configuration for connecting to SQL Server.
/// </summary>
public sealed class SqlConnectionConfig
{
    /// <summary>
    ///     Gets or sets the SQL Server instance name or address.
    /// </summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the name of the database to connect to.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the username for SQL Authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the name of the environment variable containing the password.
    /// </summary>
    public string PasswordEnvVar { get; set; } = string.Empty;

    /// <summary>
    ///     Validates that all required connection properties are configured.
    /// </summary>
    /// <returns>True if the configuration is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Server)
               && !string.IsNullOrWhiteSpace(Database)
               && !string.IsNullOrWhiteSpace(Username)
               && !string.IsNullOrWhiteSpace(PasswordEnvVar);
    }
}

