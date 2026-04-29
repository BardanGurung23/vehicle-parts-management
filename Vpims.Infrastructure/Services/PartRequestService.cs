using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Appointments;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.PartRequests;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class PartRequestService(
    IPartRequestRepository partRequestRepository,
    ICustomerRepository customerRepository) : IPartRequestService
{
    private static readonly string[] ValidStatuses = ["Pending", "Fulfilled", "Rejected"];

    public async Task<PartRequestResponse> CreatePartRequestAsync(
        UserProfileResponse currentUser,
        CreatePartRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        var partRequest = new PartRequest
        {
            CustomerId = customer.CustomerId,
            VehicleId = request.VehicleId,
            RequestedPartName = request.RequestedPartName.Trim(),
            RequestDetails = request.RequestDetails?.Trim(),
            Status = "Pending",
            RequestedAt = DateTimeOffset.UtcNow
        };

        PartRequest created = await partRequestRepository.CreateAsync(partRequest, cancellationToken);
        return ToPartRequestResponse(created, customer, null);
    }

    public async Task<IReadOnlyList<PartRequestResponse>> GetCustomerPartRequestsAsync(
        UserProfileResponse currentUser,
        CancellationToken cancellationToken = default)
    {
        Customer customer = await customerRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Customer profile not found.");

        IReadOnlyList<PartRequest> requests = await partRequestRepository.GetByCustomerIdAsync(
            customer.CustomerId,
            cancellationToken);

        return requests.Select(r => ToPartRequestResponse(r, customer, null)).ToList();
    }

    public async Task<IReadOnlyList<PartRequestResponse>> GetAllPartRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PartRequest> requests = await partRequestRepository.GetAllAsync(cancellationToken);
        return requests.Select(r => ToPartRequestResponse(r, r.Customer!, null)).ToList();
    }

    public async Task<PartRequestResponse> GetPartRequestByIdAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        PartRequest request = await partRequestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundException($"Part request with id {requestId} not found.");

        return ToPartRequestResponse(request, request.Customer!, null);
    }

    public async Task<PartRequestResponse> UpdateStatusAsync(
        int requestId,
        string status,
        CancellationToken cancellationToken = default)
    {
        PartRequest request = await partRequestRepository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundException($"Part request with id {requestId} not found.");

        string normalizedStatus = status.Trim();
        if (!ValidStatuses.Contains(normalizedStatus))
        {
            throw new AppValidationException($"Invalid status. Must be one of: {string.Join(", ", ValidStatuses)}");
        }

        request.Status = normalizedStatus;
        if (normalizedStatus is "Fulfilled" or "Rejected")
        {
            request.ResolvedAt = DateTimeOffset.UtcNow;
        }

        PartRequest updated = await partRequestRepository.UpdateAsync(request, cancellationToken);
        return ToPartRequestResponse(updated, updated.Customer!, null);
    }

    private static PartRequestResponse ToPartRequestResponse(PartRequest request, Customer customer, bool? _)
    {
        return new PartRequestResponse
        {
            RequestId = request.RequestId,
            CustomerId = request.CustomerId,
            CustomerName = customer.FullName,
            VehicleId = request.VehicleId,
            VehicleNumber = request.Vehicle?.VehicleNumber,
            RequestedPartName = request.RequestedPartName,
            RequestDetails = request.RequestDetails,
            Status = request.Status,
            RequestedAt = request.RequestedAt,
            ResolvedAt = request.ResolvedAt
        };
    }
}