namespace Vpims.Infrastructure.Persistence;

public sealed class DatabaseInitializationPlanner
{
    private const string GuidedPreflightCommand = "dotnet run --project backend/tools/Vpims.DatabasePreflight -- --apply";

    public DatabaseInitializationDecision CreateDecision(
        DatabasePreflightReport report,
        DatabaseInitializationRequest request,
        DatabaseInitializationOptions options)
    {
        if (report.HostStatus == DatabaseHostStatus.Misconfigured
            || report.HostStatus == DatabaseHostStatus.Unreachable
            || report.HostStatus == DatabaseHostStatus.AuthenticationFailed)
        {
            return Fail(report.Issues.Count > 0
                ? string.Join(" ", report.Issues)
                : "Database preflight failed before initialization could begin.");
        }

        if (report.RequiresDestructiveReset)
        {
            if (!options.AutoResetOnSchemaMismatch)
            {
                return Fail("The configured database is incompatible with the committed schema, and automatic reset handling is disabled by configuration.");
            }

            if (!request.AllowDestructiveReset)
            {
                return Fail(
                    $"The configured database '{report.TargetDatabaseName ?? "(unknown)"}' is incompatible with the committed schema and requires a destructive reset. Run '{GuidedPreflightCommand}' or 'npm run start:backend' to review and apply the guided reset workflow.");
            }

            if (options.ProtectLocalNonDemoData
                && report.LocalDataStatus != DatabaseLocalDataStatus.NoUserData
                && report.LocalDataStatus != DatabaseLocalDataStatus.DemoDataOnly
                && !request.AllowResetWithProtectedData)
            {
                return Fail(
                    "The local database contains non-demo or unclassified data. Back it up first, then rerun the preflight tool with --force-reset after reviewing the report.");
            }
        }

        return new DatabaseInitializationDecision
        {
            CanProceed = true,
            ShouldEnsureDatabaseExists = !report.DatabaseExists,
            ShouldResetDatabase = report.RequiresDestructiveReset,
            ShouldSeedDemoData = options.SeedDemoData && request.SeedDemoData
        };
    }

    private static DatabaseInitializationDecision Fail(string message)
    {
        return new DatabaseInitializationDecision
        {
            CanProceed = false,
            FailureMessage = message
        };
    }
}