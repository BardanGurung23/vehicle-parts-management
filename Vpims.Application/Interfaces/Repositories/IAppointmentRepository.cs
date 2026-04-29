using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(int appointmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Appointment> CreateAsync(Appointment appointment, CancellationToken cancellationToken = default);

    Task<Appointment> UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int appointmentId, CancellationToken cancellationToken = default);
}