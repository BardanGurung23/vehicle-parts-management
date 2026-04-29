using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Vendors;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class VendorService(IVendorRepository vendorRepository) : IVendorService
{
    public async Task<IReadOnlyList<VendorResponse>> GetAllVendorsAsync(CancellationToken cancellationToken = default)
    {
        var vendors = await vendorRepository.GetAllAsync(cancellationToken);
        return vendors.Select(ToVendorResponse).ToList();
    }

    public async Task<VendorResponse> GetVendorByIdAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        var vendor = await vendorRepository.GetByIdAsync(vendorId, cancellationToken)
            ?? throw new NotFoundException($"Vendor with id {vendorId} not found.");
        return ToVendorResponse(vendor);
    }

    public async Task<VendorResponse> CreateVendorAsync(CreateVendorRequest request, CancellationToken cancellationToken = default)
    {
        var vendor = new Vendor
        {
            VendorName = request.VendorName.Trim(),
            ContactPerson = request.ContactPerson?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var created = await vendorRepository.CreateAsync(vendor, cancellationToken);
        return ToVendorResponse(created);
    }

    public async Task<VendorResponse> UpdateVendorAsync(int vendorId, UpdateVendorRequest request, CancellationToken cancellationToken = default)
    {
        var vendor = await vendorRepository.GetByIdAsync(vendorId, cancellationToken)
            ?? throw new NotFoundException($"Vendor with id {vendorId} not found.");

        if (request.VendorName is not null)
            vendor.VendorName = request.VendorName.Trim();
        if (request.ContactPerson is not null)
            vendor.ContactPerson = request.ContactPerson.Trim();
        if (request.PhoneNumber is not null)
            vendor.PhoneNumber = request.PhoneNumber.Trim();
        if (request.Email is not null)
            vendor.Email = request.Email.Trim();
        if (request.Address is not null)
            vendor.Address = request.Address.Trim();

        var updated = await vendorRepository.UpdateAsync(vendor, cancellationToken);
        return ToVendorResponse(updated);
    }

    public async Task DeleteVendorAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        var vendor = await vendorRepository.GetByIdAsync(vendorId, cancellationToken)
            ?? throw new NotFoundException($"Vendor with id {vendorId} not found.");

        await vendorRepository.DeleteAsync(vendor, cancellationToken);
    }

    private static VendorResponse ToVendorResponse(Vendor vendor)
    {
        return new VendorResponse
        {
            VendorId = vendor.VendorId,
            VendorName = vendor.VendorName,
            ContactPerson = vendor.ContactPerson,
            PhoneNumber = vendor.PhoneNumber,
            Email = vendor.Email,
            Address = vendor.Address,
            CreatedAt = vendor.CreatedAt,
        };
    }
}
