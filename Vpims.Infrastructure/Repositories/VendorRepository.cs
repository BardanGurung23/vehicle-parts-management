using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class VendorRepository(AppDbContext dbContext) : IVendorRepository
{
    public async Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Vendors
            .AsNoTracking()
            .OrderBy(v => v.VendorName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Vendor?> GetByIdAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.VendorId == vendorId, cancellationToken);
    }

    public async Task<Vendor> CreateAsync(Vendor vendor, CancellationToken cancellationToken = default)
    {
        dbContext.Vendors.Add(vendor);
        await dbContext.SaveChangesAsync(cancellationToken);
        return vendor;
    }

    public async Task<Vendor> UpdateAsync(Vendor vendor, CancellationToken cancellationToken = default)
    {
        dbContext.Vendors.Update(vendor);
        await dbContext.SaveChangesAsync(cancellationToken);
        return vendor;
    }

    public async Task DeleteAsync(Vendor vendor, CancellationToken cancellationToken = default)
    {
        dbContext.Vendors.Remove(vendor);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int vendorId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Vendors.AnyAsync(v => v.VendorId == vendorId, cancellationToken);
    }
}
