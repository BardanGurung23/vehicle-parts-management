using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class CustomerReportRepository(AppDbContext dbContext) : ICustomerReportRepository
{
    public async Task<IReadOnlyList<Sale>> GetSalesInRangeAsync(
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Sales
            .AsNoTracking()
            .Include(sale => sale.Customer)
            .Where(sale => sale.SaleDate >= rangeStart && sale.SaleDate < rangeEndExclusive)
            .OrderByDescending(sale => sale.SaleDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetAppointmentsInRangeAsync(
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .AsNoTracking()
            .Include(appointment => appointment.Customer)
            .Where(appointment => appointment.AppointmentDate >= rangeStart && appointment.AppointmentDate < rangeEndExclusive)
            .OrderByDescending(appointment => appointment.AppointmentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sale>> GetPendingCreditSalesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Sales
            .AsNoTracking()
            .Include(sale => sale.Customer)
            .Where(sale => sale.PaymentStatus != "Paid")
            .OrderBy(sale => sale.DueDate)
            .ThenByDescending(sale => sale.SaleDate)
            .ToListAsync(cancellationToken);
    }
}