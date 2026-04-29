using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Reviews;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class ServiceReviewService(
    IServiceReviewRepository serviceReviewRepository,
    IAppointmentRepository appointmentRepository,
    ICustomerRepository customerRepository) : IServiceReviewService
{
    public async Task<ReviewResponse> CreateReviewAsync(
        UserProfileResponse currentUser,
        CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new AppValidationException("Rating must be between 1 and 5.");
        }

        Appointment appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException($"Appointment with id {request.AppointmentId} not found.");

        Customer customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        if (appointment.CustomerId != customer.CustomerId)
        {
            throw new AppValidationException("You can only review your own appointments.");
        }

        if (appointment.Status != "Completed")
        {
            throw new AppValidationException("You can only review completed appointments.");
        }

        bool alreadyReviewed = await serviceReviewRepository.ExistsByAppointmentIdAsync(request.AppointmentId, cancellationToken);
        if (alreadyReviewed)
        {
            throw new AppValidationException("This appointment has already been reviewed.");
        }

        var review = new ServiceReview
        {
            AppointmentId = request.AppointmentId,
            CustomerId = customer.CustomerId,
            Rating = request.Rating,
            Comment = request.Comment?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        ServiceReview created = await serviceReviewRepository.CreateAsync(review, cancellationToken);
        return ToReviewResponse(created, customer);
    }

    public async Task<ReviewResponse?> GetReviewByAppointmentIdAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
    {
        ServiceReview? review = await serviceReviewRepository.GetByAppointmentIdAsync(appointmentId, cancellationToken);
        return review is null ? null : ToReviewResponse(review, review.Customer!);
    }

    public async Task<IReadOnlyList<ReviewResponse>> GetCustomerReviewsAsync(
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        IReadOnlyList<ServiceReview> reviews = await serviceReviewRepository.GetByCustomerIdAsync(
            customer.CustomerId,
            cancellationToken);

        return reviews.Select(r => ToReviewResponse(r, r.Customer!)).ToList();
    }

    private static ReviewResponse ToReviewResponse(ServiceReview review, Customer customer)
    {
        return new ReviewResponse
        {
            ReviewId = review.ReviewId,
            AppointmentId = review.AppointmentId,
            CustomerId = review.CustomerId,
            CustomerName = customer.FullName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        };
    }
}