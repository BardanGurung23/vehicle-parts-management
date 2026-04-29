using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Appointments;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class AppointmentService(
    IAppointmentRepository appointmentRepository,
    ICustomerRepository customerRepository,
    IServiceReviewRepository serviceReviewRepository) : IAppointmentService
{
    private static readonly string[] ValidStatuses = ["Pending", "Confirmed", "Completed", "Cancelled"];

    public async Task<AppointmentResponse> CreateAppointmentAsync(
        UserProfileResponse currentUser,
        CreateAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        var appointment = new Appointment
        {
            CustomerId = customer.CustomerId,
            VehicleId = request.VehicleId,
            AppointmentDate = request.AppointmentDate.ToUniversalTime(),
            ServiceType = request.ServiceType.Trim(),
            Notes = request.Notes?.Trim(),
            Status = "Pending",
            CreatedAt = DateTimeOffset.UtcNow
        };

        Appointment created = await appointmentRepository.CreateAsync(appointment, cancellationToken);
        return ToAppointmentResponse(created, customer, null);
    }

    public async Task<IReadOnlyList<AppointmentResponse>> GetCustomerAppointmentsAsync(
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        IReadOnlyList<Appointment> appointments = await appointmentRepository.GetByCustomerIdAsync(
            customer.CustomerId,
            cancellationToken);

        var results = new List<AppointmentResponse>();
        foreach (var appointment in appointments)
        {
            bool hasReview = await CheckHasReviewAsync(appointment.AppointmentId, cancellationToken);
            results.Add(ToAppointmentResponse(appointment, customer, hasReview));
        }
        return results;
    }

    public async Task<IReadOnlyList<AppointmentResponse>> GetCustomerAppointmentsByCustomerIdAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        // Verify customer exists
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException($"Customer with id {customerId} not found.");

        IReadOnlyList<Appointment> appointments = await appointmentRepository.GetByCustomerIdAsync(
            customerId,
            cancellationToken);

        var results = new List<AppointmentResponse>();
        foreach (var appointment in appointments)
        {
            bool hasReview = await CheckHasReviewAsync(appointment.AppointmentId, cancellationToken);
            results.Add(ToAppointmentResponse(appointment, customer, hasReview));
        }
        return results;
    }

    public async Task<IReadOnlyList<AppointmentResponse>> GetAllAppointmentsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Appointment> appointments = await appointmentRepository.GetAllAsync(cancellationToken);
        return appointments.Select(a => ToAppointmentResponse(a, a.Customer!, null)).ToList();
    }

    public async Task<AppointmentResponse> GetAppointmentByIdAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
    {
        Appointment appointment = await appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException($"Appointment with id {appointmentId} not found.");

        bool hasReview = await CheckHasReviewAsync(appointment.AppointmentId, cancellationToken);
        return ToAppointmentResponse(appointment, appointment.Customer!, hasReview);
    }

    public async Task<AppointmentResponse> UpdateStatusAsync(
        int appointmentId,
        UpdateAppointmentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        Appointment appointment = await appointmentRepository.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException($"Appointment with id {appointmentId} not found.");

        string status = request.Status.Trim();
        if (!ValidStatuses.Contains(status))
        {
            throw new AppValidationException($"Invalid status. Must be one of: {string.Join(", ", ValidStatuses)}");
        }

        appointment.Status = status;
        Appointment updated = await appointmentRepository.UpdateAsync(appointment, cancellationToken);

        bool hasReview = await CheckHasReviewAsync(appointment.AppointmentId, cancellationToken);
        return ToAppointmentResponse(updated, updated.Customer!, hasReview);
    }

    private static AppointmentResponse ToAppointmentResponse(Appointment appointment, Customer customer, bool? hasReview)
    {
        return new AppointmentResponse
        {
            AppointmentId = appointment.AppointmentId,
            CustomerId = appointment.CustomerId,
            CustomerName = customer.FullName,
            VehicleId = appointment.VehicleId,
            VehicleNumber = appointment.Vehicle?.VehicleNumber ?? string.Empty,
            VehicleModel = appointment.Vehicle?.Model ?? string.Empty,
            AppointmentDate = appointment.AppointmentDate,
            ServiceType = appointment.ServiceType,
            Status = appointment.Status,
            Notes = appointment.Notes,
            CreatedAt = appointment.CreatedAt,
            HasReview = hasReview ?? false
        };
    }

    private async Task<bool> CheckHasReviewAsync(int appointmentId, CancellationToken cancellationToken)
    {
        return await serviceReviewRepository.ExistsByAppointmentIdAsync(appointmentId, cancellationToken);
    }
}