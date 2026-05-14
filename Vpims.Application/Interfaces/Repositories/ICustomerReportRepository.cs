using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface ICustomerReportRepository
{
    Task<IReadOnlyList<Sale>> GetSalesInRangeAsync(
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetAppointmentsInRangeAsync(
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Sale>> GetPendingCreditSalesAsync(CancellationToken cancellationToken = default);
}