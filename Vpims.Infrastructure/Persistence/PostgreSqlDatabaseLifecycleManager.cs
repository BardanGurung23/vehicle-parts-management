using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Vpims.Infrastructure.Persistence;

public sealed class PostgreSqlDatabaseLifecycleManager(AppDbContext dbContext) : IDatabaseLifecycleManager
{
    public string Provider => DatabaseInitializationOptions.PostgreSqlProvider;

    public async Task<DatabaseHostCheckResult> CheckHostAvailabilityAsync(CancellationToken cancellationToken)
    {
        try
        {
            NpgsqlConnectionStringBuilder adminBuilder = BuildAdminConnectionString();
            await using NpgsqlConnection connection = new(adminBuilder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            return new DatabaseHostCheckResult(DatabaseHostStatus.Ready);
        }
        catch (InvalidOperationException exception)
        {
            return new DatabaseHostCheckResult(DatabaseHostStatus.Misconfigured, exception.Message);
        }
        catch (Exception exception) when (IsNetworkFailure(exception))
        {
            NpgsqlConnectionStringBuilder target = GetTargetConnectionStringBuilder();
            return new DatabaseHostCheckResult(
                DatabaseHostStatus.Unreachable,
                $"Unable to reach PostgreSQL at {target.Host}:{target.Port}. Ensure PostgreSQL is installed and the service is running. {exception.Message}");
        }
        catch (Exception exception)
        {
            return new DatabaseHostCheckResult(
                DatabaseHostStatus.AuthenticationFailed,
                $"PostgreSQL is reachable, but the configured connection could not be opened. Verify the connection string credentials and permissions. {exception.Message}");
        }
    }

    public async Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken)
    {
        string databaseName = GetDatabaseName();
        NpgsqlConnectionStringBuilder adminBuilder = BuildAdminConnectionString();

        await using NpgsqlConnection connection = new(adminBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @databaseName);";
        command.Parameters.AddWithValue("databaseName", databaseName);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    public async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        if (await DatabaseExistsAsync(cancellationToken))
        {
            return;
        }

        string databaseName = GetDatabaseName();
        NpgsqlConnectionStringBuilder adminBuilder = BuildAdminConnectionString();

        await using NpgsqlConnection connection = new(adminBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using NpgsqlCommand createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)}";
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RecreateDatabaseAsync(CancellationToken cancellationToken)
    {
        string databaseName = GetDatabaseName();
        NpgsqlConnectionStringBuilder adminBuilder = BuildAdminConnectionString();

        await using NpgsqlConnection connection = new(adminBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (NpgsqlCommand terminateCommand = connection.CreateCommand())
        {
            terminateCommand.CommandText = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @databaseName AND pid <> pg_backend_pid();";
            terminateCommand.Parameters.AddWithValue("databaseName", databaseName);
            await terminateCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (NpgsqlCommand dropCommand = connection.CreateCommand())
        {
            dropCommand.CommandText = $"DROP DATABASE IF EXISTS {QuoteIdentifier(databaseName)}";
            await dropCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (NpgsqlCommand createCommand = connection.CreateCommand())
        {
            createCommand.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)}";
            await createCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public void ClearConnectionPools()
    {
        NpgsqlConnection.ClearAllPools();
    }

    private NpgsqlConnectionStringBuilder GetTargetConnectionStringBuilder()
    {
        string connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The target PostgreSQL connection string is missing.");

        return new NpgsqlConnectionStringBuilder(connectionString);
    }

    private string GetDatabaseName()
    {
        NpgsqlConnectionStringBuilder builder = GetTargetConnectionStringBuilder();
        string databaseName = builder.Database
            ?? throw new InvalidOperationException("The target PostgreSQL database name is missing from the connection string.");

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("The target PostgreSQL database name is missing from the connection string.");
        }

        return databaseName;
    }

    private NpgsqlConnectionStringBuilder BuildAdminConnectionString()
    {
        NpgsqlConnectionStringBuilder builder = GetTargetConnectionStringBuilder();
        return new NpgsqlConnectionStringBuilder(builder.ConnectionString)
        {
            Database = string.Equals(builder.Database, "postgres", StringComparison.OrdinalIgnoreCase) ? "template1" : "postgres",
            Pooling = false
        };
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    private static bool IsNetworkFailure(Exception exception)
    {
        if (exception is SocketException or TimeoutException)
        {
            return true;
        }

        return exception.InnerException is not null && IsNetworkFailure(exception.InnerException);
    }
}