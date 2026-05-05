using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface IFinancialReportRepository
{
    Task<IReadOnlyList<Sale>> GetSalesInRangeAsync(
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseInvoice>> GetPurchaseInvoicesInRangeAsync(
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive,
        CancellationToken cancellationToken = default);
}