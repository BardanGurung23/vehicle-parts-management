using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class ServiceReviewRepository(AppDbContext dbContext) : IServiceReviewRepository
{
    public async Task<ServiceReview?> GetByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ServiceReviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId, cancellationToken);
    }

    public async Task<ServiceReview?> GetByIdAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ServiceReviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceReview>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ServiceReviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Appointment)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceReview> CreateAsync(ServiceReview review, CancellationToken cancellationToken = default)
    {
        dbContext.ServiceReviews.Add(review);
        await dbContext.SaveChangesAsync(cancellationToken);
        return review;
    }

    public async Task<bool> ExistsByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ServiceReviews.AnyAsync(r => r.AppointmentId == appointmentId, cancellationToken);
    }
}