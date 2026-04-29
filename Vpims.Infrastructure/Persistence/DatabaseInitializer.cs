using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Vpims.Infrastructure.Services;

namespace Vpims.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    AppDbContext dbContext,
    IHostEnvironment environment,
    IOptions<DatabaseInitializationOptions> options,
    DemoDataSeeder demoDataSeeder,
    ILogger<DatabaseInitializer> logger)
{
    private static readonly string[] RequiredTables =
    [
        "roles",
        "users",
        "customers",
        "vehicles",
        "part_categories",
        "vendors",
        "parts",
        "appointments",
        "service_reviews",
        "part_requests",
        "predictive_alerts",
        "sales_invoices",
        "sales_invoice_items",
        "purchase_invoices",
        "purchase_invoice_items"
    ];

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        bool destructiveResetAllowed = environment.IsDevelopment() || string.Equals(environment.EnvironmentName, "Test", StringComparison.OrdinalIgnoreCase);

        if (destructiveResetAllowed)
        {
            await EnsureDatabaseExistsAsync(cancellationToken);
        }

        if (destructiveResetAllowed && options.Value.AutoResetOnSchemaMismatch)
        {
            bool requiresReset = await RequiresResetAsync(cancellationToken);
            if (requiresReset)
            {
                logger.LogWarning("Resetting local database because the schema does not match the final baseline.");
                await RecreateDatabaseAsync(cancellationToken);
            }
        }

        await dbContext.Database.MigrateAsync(cancellationToken);

        if (destructiveResetAllowed && options.Value.SeedDemoData)
        {
            await demoDataSeeder.SeedAsync(cancellationToken);
        }
    }

    private async Task<bool> RequiresResetAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            return false;
        }

        DbConnection connection = dbContext.Database.GetDbConnection();
        bool shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        bool hasUserTables = await CountAsync(connection,
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory';",
            cancellationToken) > 0;

        if (!hasUserTables)
        {
            return false;
        }

        foreach (string table in RequiredTables)
        {
            if (!await TableExistsAsync(connection, table, cancellationToken))
            {
                return true;
            }
        }

        if (!await ColumnExistsAsync(connection, "parts", "vendor_id", cancellationToken))
        {
            return true;
        }

        if (await TableExistsAsync(connection, "reviews", cancellationToken)
            || await TableExistsAsync(connection, "sales", cancellationToken)
            || await TableExistsAsync(connection, "sale_items", cancellationToken))
        {
            return true;
        }

        bool historyExists = await TableExistsAsync(connection, "__EFMigrationsHistory", cancellationToken);
        if (!historyExists)
        {
            return true;
        }

        HashSet<string> knownMigrations = dbContext.Database.GetMigrations().ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> appliedMigrations = await GetAppliedMigrationIdsAsync(connection, cancellationToken);

        bool requiresReset = appliedMigrations.Count == 0
            || appliedMigrations.Any(migrationId => !knownMigrations.Contains(migrationId));

        if (shouldCloseConnection)
        {
            await connection.CloseAsync();
        }

        return requiresReset;
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        string connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The target PostgreSQL connection string is missing.");

        NpgsqlConnectionStringBuilder builder = new(connectionString);
        string databaseName = builder.Database
            ?? throw new InvalidOperationException("The target PostgreSQL database name is missing from the connection string.");

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("The target PostgreSQL database name is missing from the connection string.");
        }

        NpgsqlConnectionStringBuilder adminBuilder = BuildAdminConnectionString(builder);

        await using NpgsqlConnection connection = new(adminBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @databaseName);";
        command.Parameters.AddWithValue("databaseName", databaseName);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        bool exists = result is bool value && value;
        if (exists)
        {
            return;
        }

        logger.LogInformation("Creating development database {DatabaseName}.", databaseName);
        await using NpgsqlCommand createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)}";
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task RecreateDatabaseAsync(CancellationToken cancellationToken)
    {
        string connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The target PostgreSQL connection string is missing.");

        NpgsqlConnectionStringBuilder builder = new(connectionString);
        string databaseName = builder.Database
            ?? throw new InvalidOperationException("The target PostgreSQL database name is missing from the connection string.");

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("The target PostgreSQL database name is missing from the connection string.");
        }

        NpgsqlConnectionStringBuilder adminBuilder = BuildAdminConnectionString(builder);

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

    private static NpgsqlConnectionStringBuilder BuildAdminConnectionString(NpgsqlConnectionStringBuilder builder)
    {
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

    private static async Task<HashSet<string>> GetAppliedMigrationIdsAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\";";

        HashSet<string> migrationIds = new(StringComparer.OrdinalIgnoreCase);
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            migrationIds.Add(reader.GetString(0));
        }

        return migrationIds;
    }

    private static async Task<bool> TableExistsAsync(DbConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @tableName);";

        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    private static async Task<bool> ColumnExistsAsync(DbConnection connection, string tableName, string columnName, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @tableName AND column_name = @columnName);";

        DbParameter tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@tableName";
        tableParameter.Value = tableName;
        command.Parameters.Add(tableParameter);

        DbParameter columnParameter = command.CreateParameter();
        columnParameter.ParameterName = "@columnName";
        columnParameter.Value = columnName;
        command.Parameters.Add(columnParameter);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool exists && exists;
    }

    private static async Task<long> CountAsync(DbConnection connection, string commandText, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }
}