using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class FinancialReportRepository(AppDbContext dbContext) : IFinancialReportRepository
{
    public async Task<IReadOnlyList<Sale>> GetSalesInRangeAsync(
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Sales
            .AsNoTracking()
            .Where(sale => sale.SaleDate >= rangeStart && sale.SaleDate < rangeEndExclusive)
            .OrderBy(sale => sale.SaleDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseInvoice>> GetPurchaseInvoicesInRangeAsync(
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PurchaseInvoices
            .AsNoTracking()
            .Where(invoice => invoice.InvoiceDate >= rangeStart && invoice.InvoiceDate < rangeEndExclusive)
            .OrderBy(invoice => invoice.InvoiceDate)
            .ToListAsync(cancellationToken);
    }
}