namespace Vpims.Infrastructure.Persistence;

public interface IDatabaseLifecycleManager
{
    string Provider { get; }

    Task<DatabaseHostCheckResult> CheckHostAvailabilityAsync(CancellationToken cancellationToken);

    Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken);

    Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken);

    Task RecreateDatabaseAsync(CancellationToken cancellationToken);

    void ClearConnectionPools();
}