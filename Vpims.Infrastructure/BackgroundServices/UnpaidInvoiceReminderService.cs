using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vpims.Application.Common;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.BackgroundServices;

/// <summary>
/// Periodically finds overdue unpaid sales invoices and sends a payment
/// reminder email to the customer associated with each invoice.
/// </summary>
public sealed class UnpaidInvoiceReminderService(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationSettings> notificationOptions,
    ILogger<UnpaidInvoiceReminderService> logger) : BackgroundService
{
    private readonly TimeSpan _interval =
        TimeSpan.FromMinutes(notificationOptions.Value.UnpaidInvoiceCheckIntervalMinutes);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "UnpaidInvoiceReminderService started. Checking every {Interval} minutes.",
            _interval.TotalMinutes);

        // Offset from the low-stock service so they don't both hit the DB at the same time.
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCheckAsync(stoppingToken);
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();

            var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            IReadOnlyList<SalesInvoice> overdueInvoices =
                await notificationRepo.GetOverdueUnpaidInvoicesAsync(cancellationToken);

            if (overdueInvoices.Count == 0)
            {
                logger.LogDebug("Unpaid invoice check: no overdue invoices found.");
                return;
            }

            int sent = 0;

            foreach (SalesInvoice invoice in overdueInvoices)
            {
                string? customerEmail = invoice.Customer?.Email;

                if (string.IsNullOrWhiteSpace(customerEmail))
                {
                    logger.LogWarning(
                        "Invoice {InvoiceNumber} is overdue but customer has no email address — skipping.",
                        invoice.InvoiceNumber);
                    continue;
                }

                string customerName = invoice.Customer?.FullName ?? "Valued Customer";
                string subject = $"[VPIMS] Payment Reminder — Invoice {invoice.InvoiceNumber}";
                string body = BuildReminderEmailBody(invoice, customerName);

                await emailService.SendAsync(customerEmail, customerName, subject, body, cancellationToken);
                sent++;
            }

            logger.LogInformation(
                "Unpaid invoice reminders sent: {Sent}/{Total} invoices processed.",
                sent, overdueInvoices.Count);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown — expected.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error in UnpaidInvoiceReminderService.");
        }
    }

    private static string BuildReminderEmailBody(SalesInvoice invoice, string customerName)
    {
        string dueDate = invoice.DueDate.HasValue
            ? invoice.DueDate.Value.ToLocalTime().ToString("dd MMM yyyy")
            : "N/A";

        string overdueDays = invoice.DueDate.HasValue
            ? $"{(int)(DateTimeOffset.UtcNow - invoice.DueDate.Value).TotalDays} day(s)"
            : "unknown";

        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:Arial,sans-serif;color:#111827;margin:0;padding:24px;">
              <h2 style="color:#d97706;">💳 Payment Reminder</h2>
              <p>Dear <strong>{customerName}</strong>,</p>
              <p>
                This is a friendly reminder that the following invoice is <strong>overdue</strong>
                by <strong>{overdueDays}</strong>. Please arrange payment at your earliest convenience.
              </p>
              <table style="border-collapse:collapse;width:100%;max-width:500px;margin:16px 0;">
                <tr style="background:#f3f4f6;">
                  <td style="padding:8px 12px;font-weight:bold;">Invoice Number</td>
                  <td style="padding:8px 12px;">{invoice.InvoiceNumber}</td>
                </tr>
                <tr>
                  <td style="padding:8px 12px;font-weight:bold;">Invoice Date</td>
                  <td style="padding:8px 12px;">{invoice.InvoiceDate.ToLocalTime():dd MMM yyyy}</td>
                </tr>
                <tr style="background:#f3f4f6;">
                  <td style="padding:8px 12px;font-weight:bold;">Due Date</td>
                  <td style="padding:8px 12px;color:#dc2626;">{dueDate}</td>
                </tr>
                <tr>
                  <td style="padding:8px 12px;font-weight:bold;">Amount Due</td>
                  <td style="padding:8px 12px;font-size:18px;font-weight:bold;color:#dc2626;">
                    Rs. {invoice.TotalAmount:N2}
                  </td>
                </tr>
              </table>
              <p>
                If you have already made the payment, please disregard this message or contact us
                so we can update your account.
              </p>
              <p style="margin-top:24px;font-size:12px;color:#6b7280;">
                This is an automated message from VPIMS. Please do not reply to this email.
              </p>
            </body>
            </html>
            """;
    }
}
