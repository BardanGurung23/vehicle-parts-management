using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface INotificationRepository
{
    /// <summary>Returns parts whose stock_quantity is at or below their reorder_level.</summary>
    Task<IReadOnlyList<Part>> GetLowStockPartsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all active Admin user email addresses.</summary>
    Task<IReadOnlyList<string>> GetAdminEmailsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns sales invoices that are still Pending and whose due date has passed.</summary>
    Task<IReadOnlyList<SalesInvoice>> GetOverdueUnpaidInvoicesAsync(CancellationToken cancellationToken = default);
}
