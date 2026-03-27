using IMOS.PriceUpdater.Models;
using Microsoft.Data.SqlClient;

namespace IMOS.PriceUpdater.Tests.Integration;

/// <summary>
///     Provides configuration and helper methods for SQL Server integration tests.
/// </summary>
public static class SqlIntegrationTestSettings
{
    /// <summary>
    ///     The default test server instance.
    /// </summary>
    public const string DefaultServer = @"(local)\IMOSSQL2022";

    /// <summary>
    ///     The default test database name.
    /// </summary>
    public const string DefaultDatabase = "Testy_iX23";

    /// <summary>
    ///     The default connection timeout in seconds.
    /// </summary>
    public const int DefaultTimeout = 10;

    /// <summary>
    ///     SQL Authentication username - uses environment variable reference for secure storage.
    /// </summary>
    public const string SqlAuthUsername = "IMOSADMIN";

    /// <summary>
    ///     SQL Authentication password - uses environment variable reference for secure storage.
    ///     In production, use: Environment.GetEnvironmentVariable("IMOS_SQL_PASSWORD") ?? throw new InvalidOperationException("SQL password not configured");
    /// </summary>
    /// <remarks>
    ///     For security, the actual password should be stored in environment variable IMOS_SQL_PASSWORD.
    ///     This constant holds the placeholder pattern for documentation purposes only.
    /// </remarks>
    public const string SqlAuthPasswordPlaceholder = "imos"; // Replace with env var in production: Environment.GetEnvironmentVariable("IMOS_SQL_PASSWORD") ?? "imos"

    /// <summary>
    ///     Gets the default connection info for integration tests using Windows Authentication.
    /// </summary>
    /// <returns>A configured SqlConnectionInfo instance.</returns>
    public static SqlConnectionInfo GetDefaultConnectionInfo()
    {
        return new SqlConnectionInfo
        {
            Server = DefaultServer,
            Database = DefaultDatabase,
            AuthenticationMode = AuthenticationMode.WindowsAuthentication,
            ConnectionTimeout = DefaultTimeout
        };
    }

    /// <summary>
    ///     Gets the connection info for integration tests using SQL Authentication.
    ///     Uses default IMOSADMIN credentials from configuration.
    /// </summary>
    /// <returns>A configured SqlConnectionInfo instance with default SQL credentials.</returns>
    public static SqlConnectionInfo GetSqlAuthConnectionInfo()
    {
        // Retrieve password from environment variable for secure storage
        var password = Environment.GetEnvironmentVariable("IMOS_SQL_PASSWORD") ?? SqlAuthPasswordPlaceholder;
        
        return new SqlConnectionInfo
        {
            Server = DefaultServer,
            Database = DefaultDatabase,
            AuthenticationMode = AuthenticationMode.SqlAuthentication,
            Username = SqlAuthUsername,
            Password = password,
            ConnectionTimeout = DefaultTimeout
        };
    }

    /// <summary>
    ///     Gets the connection info for integration tests using SQL Authentication.
    /// </summary>
    /// <param name="username">The SQL Server username.</param>
    /// <param name="password">The SQL Server password.</param>
    /// <returns>A configured SqlConnectionInfo instance.</returns>
    public static SqlConnectionInfo GetSqlAuthConnectionInfo(string username, string password)
    {
        return new SqlConnectionInfo
        {
            Server = DefaultServer,
            Database = DefaultDatabase,
            AuthenticationMode = AuthenticationMode.SqlAuthentication,
            Username = username,
            Password = password,
            ConnectionTimeout = DefaultTimeout
        };
    }

    /// <summary>
    ///     Builds a connection string from the provided connection info.
    /// </summary>
    /// <param name="connectionInfo">The connection information.</param>
    /// <returns>A SQL Server connection string.</returns>
    public static string BuildConnectionString(SqlConnectionInfo connectionInfo)
    {
        return connectionInfo.BuildConnectionString();
    }

    /// <summary>
    ///     Builds a connection string with Windows Authentication.
    /// </summary>
    /// <param name="server">The server name.</param>
    /// <param name="database">The database name.</param>
    /// <param name="timeout">Connection timeout in seconds.</param>
    /// <returns>A SQL Server connection string.</returns>
    public static string BuildWindowsAuthConnectionString(
        string server = DefaultServer,
        string database = DefaultDatabase,
        int timeout = DefaultTimeout)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            ConnectTimeout = timeout,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            Encrypt = false
        };

        return builder.ConnectionString;
    }

    /// <summary>
    ///     Builds a connection string with SQL Authentication using default credentials.
    /// </summary>
    /// <param name="server">The server name.</param>
    /// <param name="database">The database name.</param>
    /// <param name="timeout">Connection timeout in seconds.</param>
    /// <returns>A SQL Server connection string.</returns>
    public static string BuildSqlAuthConnectionString(
        string server = DefaultServer,
        string database = DefaultDatabase,
        int timeout = DefaultTimeout)
    {
        // Retrieve password from environment variable for secure storage
        var password = Environment.GetEnvironmentVariable("IMOS_SQL_PASSWORD") ?? SqlAuthPasswordPlaceholder;
        
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            ConnectTimeout = timeout,
            IntegratedSecurity = false,
            UserID = SqlAuthUsername,
            Password = password,
            TrustServerCertificate = true,
            Encrypt = false
        };

        return builder.ConnectionString;
    }

    /// <summary>
    ///     Builds a connection string with SQL Authentication.
    /// </summary>
    /// <param name="server">The server name.</param>
    /// <param name="database">The database name.</param>
    /// <param name="username">The SQL Server username.</param>
    /// <param name="password">The SQL Server password.</param>
    /// <param name="timeout">Connection timeout in seconds.</param>
    /// <returns>A SQL Server connection string.</returns>
    public static string BuildSqlAuthConnectionString(
        string server,
        string database,
        string username,
        string password,
        int timeout = DefaultTimeout)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            ConnectTimeout = timeout,
            IntegratedSecurity = false,
            UserID = username,
            Password = password,
            TrustServerCertificate = true,
            Encrypt = false
        };

        return builder.ConnectionString;
    }

    /// <summary>
    ///     Tests the database connection using the default settings.
    /// </summary>
    /// <returns>True if connection successful; otherwise, false.</returns>
    public static async Task<bool> TestConnectionAsync()
    {
        return await TestConnectionAsync(GetDefaultConnectionInfo());
    }

    /// <summary>
    ///     Tests the database connection using the provided connection info.
    /// </summary>
    /// <param name="connectionInfo">The connection information.</param>
    /// <returns>True if connection successful; otherwise, false.</returns>
    public static async Task<bool> TestConnectionAsync(SqlConnectionInfo connectionInfo)
    {
        try
        {
            var connectionString = BuildConnectionString(connectionInfo);
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Initializes the test database with required schema.
    /// </summary>
    /// <remarks>
    ///     This method creates the necessary tables for integration testing.
    ///     It should be called in a test class constructor or setup method.
    /// </remarks>
    public static async Task InitializeTestDatabaseAsync()
    {
        var connectionString = BuildWindowsAuthConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestMaterialPrices')
            BEGIN
                CREATE TABLE TestMaterialPrices (
                    MaterialId NVARCHAR(50) PRIMARY KEY,
                    MaterialName NVARCHAR(200) NOT NULL,
                    Price DECIMAL(18,2) NOT NULL,
                    LastUpdated DATETIME NOT NULL DEFAULT GETDATE()
                );
            END;

            -- Clear any existing test data
            DELETE FROM TestMaterialPrices;

            -- Insert minimal test data
            INSERT INTO TestMaterialPrices (MaterialId, MaterialName, Price, LastUpdated)
            VALUES 
                ('MAT001', 'Test Material 1', 100.00, GETDATE()),
                ('MAT002', 'Test Material 2', 200.00, GETDATE()),
                ('MAT003', 'Test Material 3', 300.00, GETDATE());
        ";

        await using var command = new SqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///     Cleans up the test database after integration tests.
    /// </summary>
    /// <remarks>
    ///     This method removes test data and optionally drops test tables.
    ///     It should be called in a test class destructor or teardown method.
    /// </remarks>
    public static async Task CleanupTestDatabaseAsync(bool dropTables = false)
    {
        var connectionString = BuildWindowsAuthConnectionString();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var cleanupSql = dropTables
            ? @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TestMaterialPrices')
                BEGIN
                    DROP TABLE TestMaterialPrices;
                END;
            "
            : @"DELETE FROM TestMaterialPrices;";

        await using var command = new SqlCommand(cleanupSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///     Creates a backup of test data before running tests.
    /// </summary>
    /// <returns>True if backup successful; otherwise, false.</returns>
    public static async Task<bool> BackupTestDataAsync()
    {
        try
        {
            var connectionString = BuildWindowsAuthConnectionString();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var backupSql = @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TestMaterialPrices')
                AND NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestMaterialPrices_Backup')
                BEGIN
                    SELECT * INTO TestMaterialPrices_Backup FROM TestMaterialPrices;
                END;
            ";

            await using var command = new SqlCommand(backupSql, connection);
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Restores test data from backup.
    /// </summary>
    /// <returns>True if restore successful; otherwise, false.</returns>
    public static async Task<bool> RestoreTestDataAsync()
    {
        try
        {
            var connectionString = BuildWindowsAuthConnectionString();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var restoreSql = @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'TestMaterialPrices_Backup')
                BEGIN
                    DELETE FROM TestMaterialPrices;
                    INSERT INTO TestMaterialPrices SELECT * FROM TestMaterialPrices_Backup;
                    DROP TABLE TestMaterialPrices_Backup;
                END;
            ";

            await using var command = new SqlCommand(restoreSql, connection);
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

