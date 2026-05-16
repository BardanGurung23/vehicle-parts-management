namespace Vpims.Infrastructure.Persistence;

public sealed class DatabaseInitializationRequest
{
    public string InvocationSource { get; init; } = "Application startup";

    public bool AllowDestructiveReset { get; init; }

    public bool AllowResetWithProtectedData { get; init; }

    public bool SeedDemoData { get; init; } = true;

    public static DatabaseInitializationRequest ForStartup()
    {
        return new DatabaseInitializationRequest();
    }

    public static DatabaseInitializationRequest ForPreflight(
        bool allowDestructiveReset,
        bool allowResetWithProtectedData = false,
        bool seedDemoData = true)
    {
        return new DatabaseInitializationRequest
        {
            InvocationSource = "Database preflight tool",
            AllowDestructiveReset = allowDestructiveReset,
            AllowResetWithProtectedData = allowResetWithProtectedData,
            SeedDemoData = seedDemoData
        };
    }
}