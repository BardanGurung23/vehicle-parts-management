using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Vpims.Application.Common;
using Vpims.Application.Interfaces;
using Vpims.Infrastructure.Options;

namespace Vpims.Infrastructure.Services;

public class SmtpEmailService(IOptions<InvoiceEmailOptions> options) : IEmailService
{
    private readonly InvoiceEmailOptions _options = options.Value;

    public async Task SendEmailAsync(string recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        ValidateSettings();

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(recipientEmail);

        using var smtpClient = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        try
        {
            await smtpClient.SendMailAsync(message, cancellationToken);
        }
        catch (SmtpException)
        {
            throw new AppValidationException("Unable to send invoice email with the current SMTP settings. Please verify the InvoiceEmail configuration.");
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_options.Host) ||
            _options.Host.Contains("example.com", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(_options.FromEmail) ||
            _options.FromEmail.Contains("example.com", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(_options.Username) ||
            _options.Username.Contains("your-smtp", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new AppValidationException("Invoice email settings are incomplete. Update the InvoiceEmail section in appsettings before sending emails.");
        }
    }
}
