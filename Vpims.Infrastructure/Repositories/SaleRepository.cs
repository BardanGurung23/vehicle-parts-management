using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class SaleRepository(AppDbContext dbContext) : ISalesRepository
{
    public async Task<IReadOnlyList<Sale>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Vehicle)
            .Include(s => s.Items)
                .ThenInclude(i => i.Part)
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sale>> GetOverdueSalesAsync(int overdueCreditMonthsThreshold, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        DateTimeOffset overdueThreshold = asOf.AddMonths(-Math.Max(0, overdueCreditMonthsThreshold));

        return await dbContext.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .Where(s => s.PaymentStatus != "Paid"
                && s.DueDate.HasValue
                && s.DueDate.Value <= overdueThreshold)
            .OrderBy(s => s.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Sale?> GetByIdAsync(int saleId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Sales
            .AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Vehicle)
            .Include(s => s.Items)
                .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(s => s.SaleId == saleId, cancellationToken);
    }

    public async Task AddAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        dbContext.Sales.Add(sale);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
