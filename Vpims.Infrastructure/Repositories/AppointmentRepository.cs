using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class AppointmentRepository(AppDbContext dbContext) : IAppointmentRepository
{
    public async Task<Appointment?> GetByIdAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId, cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .Include(a => a.Review)
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Customer)
            .Include(a => a.Vehicle)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Appointment> CreateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return appointment;
    }

    public async Task<Appointment> UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        Appointment? trackedAppointment = dbContext.Appointments.Local
            .FirstOrDefault(existing => existing.AppointmentId == appointment.AppointmentId);

        if (trackedAppointment is not null)
        {
            dbContext.Entry(trackedAppointment).CurrentValues.SetValues(appointment);
        }
        else
        {
            dbContext.Appointments.Update(appointment);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return trackedAppointment ?? appointment;
    }

    public async Task<bool> ExistsAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Appointments.AnyAsync(a => a.AppointmentId == appointmentId, cancellationToken);
    }
}