using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class PredictiveAlertRepository(AppDbContext dbContext) : IPredictiveAlertRepository
{
    public async Task<IReadOnlyList<PredictiveAlert>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PredictiveAlerts
            .AsNoTracking()
            .Include(alert => alert.Customer)
            .Include(alert => alert.Vehicle)
            .Include(alert => alert.Part)
            .Where(alert => alert.Status == "Active")
            .OrderByDescending(alert => alert.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsActiveAlertAsync(int customerId, int vehicleId, string alertMessage, CancellationToken cancellationToken = default)
    {
        return dbContext.PredictiveAlerts.AnyAsync(
            alert => alert.Status == "Active"
                && alert.CustomerId == customerId
                && alert.VehicleId == vehicleId
                && alert.AlertMessage == alertMessage,
            cancellationToken);
    }

    public async Task AddAsync(PredictiveAlert alert, CancellationToken cancellationToken = default)
    {
        dbContext.PredictiveAlerts.Add(alert);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}