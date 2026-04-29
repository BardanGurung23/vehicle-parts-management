using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface IServiceReviewRepository
{
    Task<ServiceReview?> GetByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default);

    Task<ServiceReview?> GetByIdAsync(int reviewId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceReview>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);

    Task<ServiceReview> CreateAsync(ServiceReview review, CancellationToken cancellationToken = default);

    Task<bool> ExistsByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default);
}