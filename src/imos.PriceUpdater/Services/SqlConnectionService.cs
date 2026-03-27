using IMOS.PriceUpdater.Models;
using Microsoft.Data.SqlClient;

namespace IMOS.PriceUpdater.Services;

/// <summary>
///     Implementation of SQL Server connection service supporting both
///     Windows Authentication and SQL Authentication modes.
/// </summary>
public sealed class SqlConnectionService : ISqlConnectionService
{
    private const int LoginFailedErrorNumber = 18456;
    private const int ServerNotFoundErrorNumber = 2;
    private const int TimeoutErrorNumber = -2;

    /// <inheritdoc />
    public async Task<ConnectionTestResult> TestConnectionAsync(
        SqlConnectionInfo connectionInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionInfo);

        var validationResult = ValidateConnectionInfo(connectionInfo);
        if (!validationResult.IsValid)
        {
            return ConnectionTestResult.Error(
                connectionInfo.Server,
                string.Join("; ", validationResult.Errors));
        }

        var connectionString = connectionInfo.BuildConnectionString();

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            return ConnectionTestResult.Success(connectionInfo.Server, connectionInfo.Database);
        }
        catch (SqlException ex) when (ex.Number == LoginFailedErrorNumber)
        {
            return ConnectionTestResult.InvalidCredentials(
                connectionInfo.Server,
                connectionInfo.Database);
        }
        catch (SqlException ex) when (ex.Number == ServerNotFoundErrorNumber)
        {
            return ConnectionTestResult.ServerNotFound(connectionInfo.Server);
        }
        catch (SqlException ex) when (ex.Number == TimeoutErrorNumber || ex.Message.Contains("timeout"))
        {
            return ConnectionTestResult.Timeout(
                connectionInfo.Server,
                connectionInfo.ConnectionTimeout);
        }
        catch (SqlException ex) when (ex.Message.Contains("database", StringComparison.OrdinalIgnoreCase)
                                     && (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("cannot access", StringComparison.OrdinalIgnoreCase)))
        {
            return ConnectionTestResult.DatabaseNotFound(
                connectionInfo.Server,
                connectionInfo.Database);
        }
        catch (SqlException)
        {
            return ConnectionTestResult.NetworkError(connectionInfo.Server);
        }
        catch (OperationCanceledException)
        {
            return ConnectionTestResult.Error(
                connectionInfo.Server,
                "Connection was cancelled.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetDatabasesAsync(
        SqlConnectionInfo connectionInfo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionInfo);

        // Create a connection string without specifying a database
        var masterConnectionInfo = new SqlConnectionInfo
        {
            Server = connectionInfo.Server,
            Database = "master",
            AuthenticationMode = connectionInfo.AuthenticationMode,
            Username = connectionInfo.Username,
            Password = connectionInfo.Password,
            ConnectionTimeout = connectionInfo.ConnectionTimeout
        };

        var connectionString = masterConnectionInfo.BuildConnectionString();
        var databases = new List<string>();

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new SqlCommand(
                "SELECT name FROM sys.databases WHERE state = 0 ORDER BY name",
                connection);
            command.CommandTimeout = connectionInfo.ConnectionTimeout;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var dbName = reader.GetString(0);
                databases.Add(dbName);
            }
        }
        catch (SqlException)
        {
            // Return empty list if we can't get databases
        }

        return databases.AsReadOnly();
    }

    /// <inheritdoc />
    public ValidationResult ValidateConnectionInfo(SqlConnectionInfo connectionInfo)
    {
        ArgumentNullException.ThrowIfNull(connectionInfo);
        return connectionInfo.Validate();
    }
}

