namespace Vpims.Application.Interfaces.Services;

public interface IEmailService
{
    /// <summary>Sends a plain-text / HTML email.</summary>
    Task SendAsync(
        string toAddress,
        string toDisplayName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
