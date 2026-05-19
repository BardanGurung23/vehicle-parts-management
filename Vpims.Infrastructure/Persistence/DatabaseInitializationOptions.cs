namespace Vpims.Infrastructure.Persistence;

public sealed class DatabaseInitializationOptions
{
    public const string SectionName = "DatabaseInitialization";

    public const string PostgreSqlProvider = "Postgres";

    public const string InMemoryProvider = "InMemory";

    public string Provider { get; set; } = PostgreSqlProvider;

    public string InMemoryDatabaseName { get; set; } = "AutonixDevDb";

    public bool ProtectLocalNonDemoData { get; set; } = true;

    public string[] ProtectedDemoUserEmails { get; set; } =
    [
        "demo.admin1@autonix.local",
        "demo.admin2@autonix.local",
        "demo.staff1@autonix.local",
        "demo.staff2@autonix.local",
        "demo.customer1@autonix.local",
        "demo.customer2@autonix.local",
        "demo.customer3@autonix.local"
    ];

    public bool AutoResetOnSchemaMismatch { get; set; } = true;

    public bool SeedDemoData { get; set; } = true;
}