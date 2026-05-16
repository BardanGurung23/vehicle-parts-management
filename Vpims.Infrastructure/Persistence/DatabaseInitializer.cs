using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vpims.Infrastructure.Data;
using Vpims.Infrastructure.Services;

namespace Vpims.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    AppDbContext dbContext,
    VpimsDbContext vpimsDbContext,
    IOptions<DatabaseInitializationOptions> options,
    DemoDataSeeder demoDataSeeder,
    VpimsDbSeeder vpimsDbSeeder,
    IDatabaseLifecycleManager databaseLifecycleManager,
    DatabaseSchemaCompatibilityEvaluator schemaCompatibilityEvaluator,
    DatabaseLocalDataSafetyEvaluator localDataSafetyEvaluator,
    DatabaseInitializationPlanner planner,
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
        "purchase_invoice_items",
        "staff_customers",
        "staff_vehicle_parts",
        "staff_sales",
        "staff_sale_items"
    ];

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return InitializeAsync(DatabaseInitializationRequest.ForStartup(), cancellationToken);
    }

    public async Task InitializeAsync(
        DatabaseInitializationRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Starting database initialization from {InvocationSource} using {Provider}.",
            request.InvocationSource,
            databaseLifecycleManager.Provider);

        DatabasePreflightReport report = await CreatePreflightReportAsync(cancellationToken);
        DatabaseInitializationDecision decision = planner.CreateDecision(report, request, options.Value);

        if (!decision.CanProceed)
        {
            logger.LogError("Database initialization aborted: {FailureMessage}", decision.FailureMessage);
            throw new InvalidOperationException(decision.FailureMessage);
        }

        if (decision.ShouldEnsureDatabaseExists)
        {
            logger.LogInformation("Creating database {DatabaseName} before migration.", report.TargetDatabaseName);
            await databaseLifecycleManager.EnsureDatabaseExistsAsync(cancellationToken);
        }

        if (decision.ShouldResetDatabase)
        {
            logger.LogWarning(
                "Recreating database {DatabaseName} because the local schema is incompatible with the committed baseline.",
                report.TargetDatabaseName);

            await databaseLifecycleManager.RecreateDatabaseAsync(cancellationToken);
            databaseLifecycleManager.ClearConnectionPools();
        }

        if (string.Equals(databaseLifecycleManager.Provider, DatabaseInitializationOptions.InMemoryProvider, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Ensuring in-memory database instances are created.");
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            await vpimsDbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
        else
        {
            logger.LogInformation("Ensuring staff sales schema exists.");
            await vpimsDbContext.Database.EnsureCreatedAsync(cancellationToken);

            logger.LogInformation("Applying EF Core migrations.");
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        if (decision.ShouldSeedDemoData)
        {
            logger.LogInformation("Seeding canonical demo data.");
            await demoDataSeeder.SeedAsync(cancellationToken);
            await vpimsDbSeeder.SeedAsync(cancellationToken);
        }

        logger.LogInformation("Running post-initialization validation.");
        DatabasePreflightReport finalReport = await CreatePreflightReportAsync(cancellationToken);
        if (!finalReport.CanProceedWithoutReset)
        {
            string validationMessage = BuildValidationFailureMessage(finalReport);
            logger.LogError("Database post-validation failed: {ValidationMessage}", validationMessage);
            throw new InvalidOperationException(validationMessage);
        }

        logger.LogInformation("Database initialization completed successfully.");
    }

    public async Task<DatabasePreflightReport> CreatePreflightReportAsync(CancellationToken cancellationToken = default)
    {
        if (string.Equals(databaseLifecycleManager.Provider, DatabaseInitializationOptions.InMemoryProvider, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Skipping external database checks because the in-memory provider is configured.");
            return new DatabasePreflightReport
            {
                Provider = databaseLifecycleManager.Provider,
                HostStatus = DatabaseHostStatus.NotRequired,
                DatabaseExists = true,
                SchemaStatus = DatabaseSchemaCompatibilityStatus.Compatible,
                LocalDataStatus = DatabaseLocalDataStatus.NotChecked,
                Notes = ["The in-memory provider is configured. External database validation is not required."]
            };
        }

        string? databaseName = GetTargetDatabaseName();
        DatabaseHostCheckResult hostCheck = await databaseLifecycleManager.CheckHostAvailabilityAsync(cancellationToken);
        if (hostCheck.Status != DatabaseHostStatus.Ready)
        {
            logger.LogWarning("Database host availability check failed: {Message}", hostCheck.Message);
            return new DatabasePreflightReport
            {
                Provider = databaseLifecycleManager.Provider,
                TargetDatabaseName = databaseName,
                HostStatus = hostCheck.Status,
                Issues = hostCheck.Message is null ? [] : [hostCheck.Message]
            };
        }

        bool databaseExists = await databaseLifecycleManager.DatabaseExistsAsync(cancellationToken);
        if (!databaseExists)
        {
            logger.LogInformation("Target database {DatabaseName} does not exist yet.", databaseName);
            return new DatabasePreflightReport
            {
                Provider = databaseLifecycleManager.Provider,
                TargetDatabaseName = databaseName,
                HostStatus = DatabaseHostStatus.Ready,
                DatabaseExists = false,
                SchemaStatus = DatabaseSchemaCompatibilityStatus.Empty,
                LocalDataStatus = DatabaseLocalDataStatus.NoUserData,
                Notes = ["The configured database does not exist yet and will be created during initialization."]
            };
        }

        DatabaseSchemaInspection inspection = await InspectExistingSchemaAsync(cancellationToken);
        DatabaseSchemaCompatibilityStatus schemaStatus = schemaCompatibilityEvaluator.Evaluate(inspection);
        DatabaseLocalDataStatus localDataStatus = DatabaseLocalDataStatus.NotChecked;

        if (schemaStatus == DatabaseSchemaCompatibilityStatus.Incompatible)
        {
            localDataStatus = await InspectLocalDataStatusAsync(cancellationToken);
        }

        return new DatabasePreflightReport
        {
            Provider = databaseLifecycleManager.Provider,
            TargetDatabaseName = databaseName,
            HostStatus = DatabaseHostStatus.Ready,
            DatabaseExists = true,
            SchemaStatus = schemaStatus,
            LocalDataStatus = localDataStatus,
            Issues = BuildIssues(inspection, localDataStatus),
            Notes = schemaStatus == DatabaseSchemaCompatibilityStatus.Compatible
                ? ["The configured database matches the committed schema baseline."]
                : []
        };
    }

    private async Task<DatabaseSchemaInspection> InspectExistingSchemaAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            return new DatabaseSchemaInspection
            {
                InspectionSucceeded = false,
                InspectionError = "The configured database exists but the application could not open it for schema inspection."
            };
        }

        DbConnection connection = dbContext.Database.GetDbConnection();
        bool shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            bool hasUserTables = await CountAsync(
                connection,
                "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory';",
                cancellationToken) > 0;

            if (!hasUserTables)
            {
                return new DatabaseSchemaInspection
                {
                    HasUserTables = false
                };
            }

            List<string> missingRequiredTables = [];
            foreach (string table in RequiredTables)
            {
                if (!await TableExistsAsync(connection, table, cancellationToken))
                {
                    missingRequiredTables.Add(table);
                }
            }

            List<string> missingRequiredColumns = [];
            if (!await ColumnExistsAsync(connection, "parts", "vendor_id", cancellationToken))
            {
                missingRequiredColumns.Add("parts.vendor_id");
            }

            List<string> deprecatedTables = [];
            if (await TableExistsAsync(connection, "reviews", cancellationToken))
            {
                deprecatedTables.Add("reviews");
            }

            if (await TableExistsAsync(connection, "sales", cancellationToken))
            {
                deprecatedTables.Add("sales");
            }

            if (await TableExistsAsync(connection, "sale_items", cancellationToken))
            {
                deprecatedTables.Add("sale_items");
            }

            bool historyExists = await TableExistsAsync(connection, "__EFMigrationsHistory", cancellationToken);
            HashSet<string> knownMigrations = dbContext.Database.GetMigrations().ToHashSet(StringComparer.OrdinalIgnoreCase);
            HashSet<string> appliedMigrations = historyExists
                ? await GetAppliedMigrationIdsAsync(connection, cancellationToken)
                : [];

            return new DatabaseSchemaInspection
            {
                HasUserTables = true,
                MissingRequiredTables = missingRequiredTables,
                MissingRequiredColumns = missingRequiredColumns,
                DeprecatedTablesPresent = deprecatedTables,
                MigrationHistoryExists = historyExists,
                AppliedMigrationCount = appliedMigrations.Count,
                UnknownAppliedMigrations = appliedMigrations
                    .Where(migrationId => !knownMigrations.Contains(migrationId))
                    .ToArray()
            };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to inspect the existing database schema.");
            return new DatabaseSchemaInspection
            {
                InspectionSucceeded = false,
                InspectionError = exception.Message
            };
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<DatabaseLocalDataStatus> InspectLocalDataStatusAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        bool shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;
        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            if (!await TableExistsAsync(connection, "users", cancellationToken)
                || !await ColumnExistsAsync(connection, "users", "email", cancellationToken))
            {
                return DatabaseLocalDataStatus.Unknown;
            }

            IReadOnlyCollection<string> persistedEmails = await GetUserEmailsAsync(connection, cancellationToken);
            return localDataSafetyEvaluator.Evaluate(persistedEmails);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to classify existing local data before a potential reset.");
            return DatabaseLocalDataStatus.Unknown;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
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

    private static async Task<IReadOnlyCollection<string>> GetUserEmailsAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT email FROM users WHERE email IS NOT NULL;";

        List<string> emails = [];
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            emails.Add(reader.GetString(0));
        }

        return emails;
    }

    private static async Task<long> CountAsync(DbConnection connection, string commandText, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result);
    }

    private static string BuildValidationFailureMessage(DatabasePreflightReport report)
    {
        if (report.Issues.Count == 0)
        {
            return "Database validation failed after initialization for an unknown reason.";
        }

        return $"Database validation failed after initialization: {string.Join(" ", report.Issues)}";
    }

    private string? GetTargetDatabaseName()
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        return string.IsNullOrWhiteSpace(connection.Database) ? null : connection.Database;
    }

    private static IReadOnlyList<string> BuildIssues(
        DatabaseSchemaInspection inspection,
        DatabaseLocalDataStatus localDataStatus)
    {
        List<string> issues = [];

        if (!inspection.InspectionSucceeded && !string.IsNullOrWhiteSpace(inspection.InspectionError))
        {
            issues.Add(inspection.InspectionError);
        }

        if (inspection.MissingRequiredTables.Count > 0)
        {
            issues.Add($"Missing required tables: {string.Join(", ", inspection.MissingRequiredTables)}.");
        }

        if (inspection.MissingRequiredColumns.Count > 0)
        {
            issues.Add($"Missing required columns: {string.Join(", ", inspection.MissingRequiredColumns)}.");
        }

        if (inspection.DeprecatedTablesPresent.Count > 0)
        {
            issues.Add($"Deprecated tables were detected: {string.Join(", ", inspection.DeprecatedTablesPresent)}.");
        }

        if (inspection.HasUserTables && !inspection.MigrationHistoryExists)
        {
            issues.Add("The __EFMigrationsHistory table is missing.");
        }

        if (inspection.MigrationHistoryExists && inspection.AppliedMigrationCount == 0)
        {
            issues.Add("No EF Core migrations have been applied to the existing database.");
        }

        if (inspection.UnknownAppliedMigrations.Count > 0)
        {
            issues.Add($"Unknown applied migrations were detected: {string.Join(", ", inspection.UnknownAppliedMigrations)}.");
        }

        if (localDataStatus == DatabaseLocalDataStatus.ContainsNonDemoData)
        {
            issues.Add("Reset protection blocked the reset because non-demo local users were detected.");
        }

        if (localDataStatus == DatabaseLocalDataStatus.Unknown)
        {
            issues.Add("Reset protection could not classify the existing local data safely.");
        }

        return issues;
    }
}