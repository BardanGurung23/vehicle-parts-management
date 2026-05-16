namespace Vpims.Infrastructure.Persistence;

public sealed class InMemoryDatabaseLifecycleManager : IDatabaseLifecycleManager
{
    public string Provider => DatabaseInitializationOptions.InMemoryProvider;

    public Task<DatabaseHostCheckResult> CheckHostAvailabilityAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new DatabaseHostCheckResult(
            DatabaseHostStatus.NotRequired,
            "The in-memory provider does not require an external database service."));
    }

    public Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task RecreateDatabaseAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void ClearConnectionPools()
    {
    }
}