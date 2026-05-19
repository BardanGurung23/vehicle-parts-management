using Vpims.Application.DTOs.VehicleInsights;

namespace Vpims.Application.Interfaces.Services;

public interface IAiVehicleInsightsService
{
    Task<VehicleInsightsResponse> GetVehicleInsightsAsync(
        int customerId,
        int vehicleId,
        CancellationToken cancellationToken = default);
}