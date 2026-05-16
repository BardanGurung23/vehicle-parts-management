namespace Vpims.Infrastructure.Persistence;

public enum DatabaseHostStatus
{
    NotRequired,
    Ready,
    Unreachable,
    AuthenticationFailed,
    Misconfigured
}

public enum DatabaseSchemaCompatibilityStatus
{
    NotChecked,
    Empty,
    Compatible,
    Incompatible
}

public enum DatabaseLocalDataStatus
{
    NotChecked,
    NoUserData,
    DemoDataOnly,
    ContainsNonDemoData,
    Unknown
}

public sealed record DatabaseHostCheckResult(DatabaseHostStatus Status, string? Message = null);

public sealed record DatabaseSchemaInspection
{
    public bool InspectionSucceeded { get; init; } = true;

    public string? InspectionError { get; init; }

    public bool HasUserTables { get; init; }

    public IReadOnlyCollection<string> MissingRequiredTables { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> MissingRequiredColumns { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> DeprecatedTablesPresent { get; init; } = Array.Empty<string>();

    public bool MigrationHistoryExists { get; init; }

    public int AppliedMigrationCount { get; init; }

    public IReadOnlyCollection<string> UnknownAppliedMigrations { get; init; } = Array.Empty<string>();
}

public sealed record DatabasePreflightReport
{
    public string Provider { get; init; } = DatabaseInitializationOptions.PostgreSqlProvider;

    public string? TargetDatabaseName { get; init; }

    public DatabaseHostStatus HostStatus { get; init; } = DatabaseHostStatus.NotRequired;

    public bool DatabaseExists { get; init; }

    public DatabaseSchemaCompatibilityStatus SchemaStatus { get; init; } = DatabaseSchemaCompatibilityStatus.NotChecked;

    public DatabaseLocalDataStatus LocalDataStatus { get; init; } = DatabaseLocalDataStatus.NotChecked;

    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();

    public bool RequiresDestructiveReset =>
        DatabaseExists && SchemaStatus == DatabaseSchemaCompatibilityStatus.Incompatible;

    public bool CanProceedWithoutReset =>
        (HostStatus == DatabaseHostStatus.NotRequired || HostStatus == DatabaseHostStatus.Ready)
        && (!DatabaseExists
            || SchemaStatus == DatabaseSchemaCompatibilityStatus.Empty
            || SchemaStatus == DatabaseSchemaCompatibilityStatus.Compatible);
}

public sealed record DatabaseInitializationDecision
{
    public bool CanProceed { get; init; }

    public bool ShouldEnsureDatabaseExists { get; init; }

    public bool ShouldResetDatabase { get; init; }

    public bool ShouldSeedDemoData { get; init; }

    public string FailureMessage { get; init; } = string.Empty;
}