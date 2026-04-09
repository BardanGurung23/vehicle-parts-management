using Vpims.Application.DTOs.Customers;

namespace Vpims.Application.Interfaces.Services;

public interface ICustomerService
{
    Task<RegisterCustomerResponse> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default);
}