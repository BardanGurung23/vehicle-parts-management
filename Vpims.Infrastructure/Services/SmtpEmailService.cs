using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vpims.Application.Common;
using Vpims.Application.Interfaces.Services;

namespace Vpims.Infrastructure.Services;

public sealed class SmtpEmailService(
    IOptions<EmailSettings> emailOptions,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailSettings _settings = emailOptions.Value;

    public async Task SendAsync(
        string toAddress,
        string toDisplayName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
        };

        var from = new MailAddress(_settings.FromAddress, _settings.FromDisplayName);
        var to = new MailAddress(toAddress, toDisplayName);

        using var message = new MailMessage(from, to)
        {
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("Email sent to {To} — subject: {Subject}", toAddress, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To} — subject: {Subject}", toAddress, subject);
            // Swallow so one bad address doesn't abort the whole notification run.
        }
    }
}
