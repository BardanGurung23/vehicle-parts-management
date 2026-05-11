namespace Vpims.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken);
}
