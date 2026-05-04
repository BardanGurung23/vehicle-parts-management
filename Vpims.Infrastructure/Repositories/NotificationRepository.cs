using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class NotificationRepository(AppDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<Part>> GetLowStockPartsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Parts
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.StockQuantity <= p.ReorderLevel)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAdminEmailsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.IsActive && u.Role != null && u.Role.Name == SystemRoles.Admin)
            .Select(u => u.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SalesInvoice>> GetOverdueUnpaidInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await dbContext.SalesInvoices
            .AsNoTracking()
            .Include(s => s.Customer)
            .Where(s => s.PaymentStatus == "Pending"
                     && s.DueDate.HasValue
                     && s.DueDate.Value < now)
            .OrderBy(s => s.DueDate)
            .ToListAsync(cancellationToken);
    }
}
