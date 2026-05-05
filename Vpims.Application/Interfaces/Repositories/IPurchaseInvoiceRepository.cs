using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface IPurchaseInvoiceRepository
{
    Task<IReadOnlyList<PurchaseInvoice>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PurchaseInvoice?> GetByIdAsync(int purchaseInvoiceId, CancellationToken cancellationToken = default);

    Task<PurchaseInvoice> CreateAsync(PurchaseInvoice purchaseInvoice, CancellationToken cancellationToken = default);
}