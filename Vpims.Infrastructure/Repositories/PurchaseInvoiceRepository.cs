using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class PurchaseInvoiceRepository(AppDbContext dbContext) : IPurchaseInvoiceRepository
{
    public async Task<IReadOnlyList<PurchaseInvoice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PurchaseInvoices
            .AsNoTracking()
            .Include(invoice => invoice.Vendor)
            .Include(invoice => invoice.CreatedByUser)
            .Include(invoice => invoice.Items)
                .ThenInclude(item => item.Part)
            .OrderByDescending(invoice => invoice.InvoiceDate)
            .ThenByDescending(invoice => invoice.PurchaseInvoiceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<PurchaseInvoice?> GetByIdAsync(int purchaseInvoiceId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PurchaseInvoices
            .AsNoTracking()
            .Include(invoice => invoice.Vendor)
            .Include(invoice => invoice.CreatedByUser)
            .Include(invoice => invoice.Items)
                .ThenInclude(item => item.Part)
            .FirstOrDefaultAsync(invoice => invoice.PurchaseInvoiceId == purchaseInvoiceId, cancellationToken);
    }

    public async Task<PurchaseInvoice> CreateAsync(PurchaseInvoice purchaseInvoice, CancellationToken cancellationToken = default)
    {
        dbContext.PurchaseInvoices.Add(purchaseInvoice);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(purchaseInvoice.PurchaseInvoiceId, cancellationToken))!;
    }
}