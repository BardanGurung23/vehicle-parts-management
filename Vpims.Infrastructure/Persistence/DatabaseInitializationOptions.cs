namespace Vpims.Infrastructure.Persistence;

public sealed class DatabaseInitializationOptions
{
    public const string SectionName = "DatabaseInitialization";

    public bool AutoResetOnSchemaMismatch { get; set; } = true;

    public bool SeedDemoData { get; set; } = true;
}