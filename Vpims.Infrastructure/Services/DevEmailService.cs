using System.Net;
using Vpims.Application.Common;
using Vpims.Application.DTOs.Dev;
using Vpims.Application.Interfaces;
using Vpims.Application.Interfaces.Services;

namespace Vpims.Infrastructure.Services;

public sealed class DevEmailService(IEmailService emailService) : IDevEmailService
{
    public async Task<TestEmailResponse> SendTestEmailAsync(TestEmailRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new AppValidationException("A test email request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            throw new AppValidationException("A recipient email address is required before sending a test email.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new AppValidationException("A test email subject is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new AppValidationException("A test email message is required.");
        }

        string recipientEmail = request.RecipientEmail.Trim();
        string subject = request.Subject.Trim();
        string encodedMessage = WebUtility.HtmlEncode(request.Message.Trim())
            .Replace("\r\n", "<br />", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal);
        string htmlBody = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;color:#1f2933;line-height:1.5;">
                <h2>Autonix test email</h2>
                <p>This message was triggered from the development test endpoint.</p>
                <p>{encodedMessage}</p>
            </div>
            """;

        await emailService.SendEmailAsync(recipientEmail, subject, htmlBody, cancellationToken);

        return new TestEmailResponse
        {
            RecipientEmail = recipientEmail,
            Subject = subject,
            Message = "Test email sent successfully."
        };
    }
}