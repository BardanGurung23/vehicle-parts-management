using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface IVendorRepository
{
    Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Vendor?> GetByIdAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<Vendor> CreateAsync(Vendor vendor, CancellationToken cancellationToken = default);
    Task<Vendor> UpdateAsync(Vendor vendor, CancellationToken cancellationToken = default);
    Task DeleteAsync(Vendor vendor, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int vendorId, CancellationToken cancellationToken = default);
}
