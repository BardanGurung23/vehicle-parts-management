using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface IPartRequestRepository
{
    Task<PartRequest?> GetByIdAsync(int requestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartRequest>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PartRequest>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PartRequest> CreateAsync(PartRequest partRequest, CancellationToken cancellationToken = default);

    Task<PartRequest> UpdateAsync(PartRequest partRequest, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int requestId, CancellationToken cancellationToken = default);
}