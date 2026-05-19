using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface ISalesRepository
{
    Task<IReadOnlyList<Sale>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<Sale?> GetByIdAsync(int saleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sale>> GetOverdueSalesAsync(int overdueCreditMonthsThreshold, DateTimeOffset asOf, CancellationToken cancellationToken = default);
    Task AddAsync(Sale sale, CancellationToken cancellationToken = default);
}
