using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface IPredictiveAlertRepository
{
    Task<IReadOnlyList<PredictiveAlert>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveAlertAsync(int customerId, int vehicleId, string alertMessage, CancellationToken cancellationToken = default);
    Task AddAsync(PredictiveAlert alert, CancellationToken cancellationToken = default);
}