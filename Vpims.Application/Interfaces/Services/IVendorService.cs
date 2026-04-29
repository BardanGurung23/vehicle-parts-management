using Vpims.Application.DTOs.Vendors;
using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Services;

public interface IVendorService
{
    Task<IReadOnlyList<VendorResponse>> GetAllVendorsAsync(CancellationToken cancellationToken = default);
    Task<VendorResponse> GetVendorByIdAsync(int vendorId, CancellationToken cancellationToken = default);
    Task<VendorResponse> CreateVendorAsync(CreateVendorRequest request, CancellationToken cancellationToken = default);
    Task<VendorResponse> UpdateVendorAsync(int vendorId, UpdateVendorRequest request, CancellationToken cancellationToken = default);
    Task DeleteVendorAsync(int vendorId, CancellationToken cancellationToken = default);
}
