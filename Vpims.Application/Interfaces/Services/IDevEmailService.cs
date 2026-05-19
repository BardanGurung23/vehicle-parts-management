using Vpims.Application.DTOs.Dev;

namespace Vpims.Application.Interfaces.Services;

public interface IDevEmailService
{
    Task<TestEmailResponse> SendTestEmailAsync(TestEmailRequest request, CancellationToken cancellationToken = default);
}