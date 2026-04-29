using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class PartRequestRepository(AppDbContext dbContext) : IPartRequestRepository
{
    public async Task<PartRequest?> GetByIdAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PartRequests
            .AsNoTracking()
            .Include(pr => pr.Customer)
            .Include(pr => pr.Vehicle)
            .FirstOrDefaultAsync(pr => pr.RequestId == requestId, cancellationToken);
    }

    public async Task<IReadOnlyList<PartRequest>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PartRequests
            .AsNoTracking()
            .Include(pr => pr.Vehicle)
            .Where(pr => pr.CustomerId == customerId)
            .OrderByDescending(pr => pr.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PartRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PartRequests
            .AsNoTracking()
            .Include(pr => pr.Customer)
            .Include(pr => pr.Vehicle)
            .OrderByDescending(pr => pr.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PartRequest> CreateAsync(PartRequest partRequest, CancellationToken cancellationToken = default)
    {
        dbContext.PartRequests.Add(partRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
        return partRequest;
    }

    public async Task<PartRequest> UpdateAsync(PartRequest partRequest, CancellationToken cancellationToken = default)
    {
        dbContext.PartRequests.Update(partRequest);
        await dbContext.SaveChangesAsync(cancellationToken);
        return partRequest;
    }

    public async Task<bool> ExistsAsync(int requestId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PartRequests.AnyAsync(pr => pr.RequestId == requestId, cancellationToken);
    }
}