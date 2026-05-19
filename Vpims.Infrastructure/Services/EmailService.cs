using System.IO;
using System.Net.Sockets;
using SystemMailAddress = System.Net.Mail.MailAddress;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Vpims.Application.Common;
using Vpims.Application.Interfaces;
using Vpims.Infrastructure.Options;

namespace Vpims.Infrastructure.Services;

public sealed class EmailService(IOptions<InvoiceEmailOptions> options) : IEmailService
{
    private const string InvalidConfigurationMessage = "Invoice email settings are incomplete. Update the InvoiceEmail section in appsettings before sending emails.";
    private const string InvalidCredentialPairMessage = "Invoice email settings are incomplete. Provide both InvoiceEmail username and password, or leave both empty.";
    private const string DeliveryFailureMessage = "Unable to send email with the current SMTP settings. Please verify the InvoiceEmail configuration.";

    private readonly InvoiceEmailOptions emailOptions = options.Value;

    public async Task SendEmailAsync(string recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        ValidateSettings();

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new AppValidationException("A recipient email address is required before sending email.");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new AppValidationException("An email subject is required before sending email.");
        }

        if (string.IsNullOrWhiteSpace(htmlBody))
        {
            throw new AppValidationException("An email body is required before sending email.");
        }

        string normalizedRecipientEmail = recipientEmail.Trim();

        if (!IsValidEmailAddress(normalizedRecipientEmail))
        {
            throw new AppValidationException("The recipient email address is not valid.");
        }

        string normalizedFromEmail = emailOptions.FromEmail.Trim();

        if (!IsValidEmailAddress(normalizedFromEmail))
        {
            throw new AppValidationException(InvalidConfigurationMessage);
        }

        MailboxAddress recipientAddress = MailboxAddress.Parse(normalizedRecipientEmail);
        MailboxAddress fromAddress = MailboxAddress.Parse(normalizedFromEmail);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(ResolveFromName(), fromAddress.Address));
        message.To.Add(recipientAddress);
        message.Subject = subject.Trim();
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody
        }.ToMessageBody();

        using var smtpClient = new SmtpClient();

        try
        {
            await smtpClient.ConnectAsync(
                emailOptions.Host.Trim(),
                emailOptions.Port,
                ResolveSocketOptions(emailOptions.Port, emailOptions.EnableSsl),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(emailOptions.Username))
            {
                await smtpClient.AuthenticateAsync(
                    emailOptions.Username.Trim(),
                    emailOptions.Password!,
                    cancellationToken);
            }

            await smtpClient.SendAsync(message, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);
        }
        catch (AppValidationException)
        {
            throw;
        }
        catch (MailKit.Security.AuthenticationException)
        {
            throw new AppValidationException(DeliveryFailureMessage);
        }
        catch (IOException)
        {
            throw new AppValidationException(DeliveryFailureMessage);
        }
        catch (SocketException)
        {
            throw new AppValidationException(DeliveryFailureMessage);
        }
        catch (SmtpCommandException)
        {
            throw new AppValidationException(DeliveryFailureMessage);
        }
        catch (SmtpProtocolException)
        {
            throw new AppValidationException(DeliveryFailureMessage);
        }
    }

    private string ResolveFromName()
    {
        return string.IsNullOrWhiteSpace(emailOptions.FromName)
            ? "Autonix"
            : emailOptions.FromName.Trim();
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(emailOptions.Host) ||
            emailOptions.Port <= 0 ||
            string.IsNullOrWhiteSpace(emailOptions.FromEmail))
        {
            throw new AppValidationException(InvalidConfigurationMessage);
        }

        bool hasUsername = !string.IsNullOrWhiteSpace(emailOptions.Username);
        bool hasPassword = !string.IsNullOrWhiteSpace(emailOptions.Password);

        if (hasUsername != hasPassword)
        {
            throw new AppValidationException(InvalidCredentialPairMessage);
        }
    }

    private static SecureSocketOptions ResolveSocketOptions(int port, bool enableSsl)
    {
        if (!enableSsl)
        {
            return SecureSocketOptions.None;
        }

        return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
    }

    private static bool IsValidEmailAddress(string emailAddress)
    {
        try
        {
            _ = new SystemMailAddress(emailAddress);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}