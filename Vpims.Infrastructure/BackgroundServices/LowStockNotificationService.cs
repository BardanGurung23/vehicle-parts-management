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
/// Periodically checks for parts at or below their reorder level and
/// sends a consolidated alert email to every active Admin user.
/// </summary>
public sealed class LowStockNotificationService(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationSettings> notificationOptions,
    ILogger<LowStockNotificationService> logger) : BackgroundService
{
    private readonly TimeSpan _interval =
        TimeSpan.FromMinutes(notificationOptions.Value.LowStockCheckIntervalMinutes);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "LowStockNotificationService started. Checking every {Interval} minutes.",
            _interval.TotalMinutes);

        // Small initial delay so the app finishes starting up first.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

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

            IReadOnlyList<Part> lowStockParts =
                await notificationRepo.GetLowStockPartsAsync(cancellationToken);

            if (lowStockParts.Count == 0)
            {
                logger.LogDebug("Low-stock check: all parts are adequately stocked.");
                return;
            }

            IReadOnlyList<string> adminEmails =
                await notificationRepo.GetAdminEmailsAsync(cancellationToken);

            if (adminEmails.Count == 0)
            {
                logger.LogWarning("Low-stock check: {Count} low-stock parts found but no Admin emails configured.", lowStockParts.Count);
                return;
            }

            string subject = $"[VPIMS] Low Stock Alert — {lowStockParts.Count} part(s) need restocking";
            string body = BuildLowStockEmailBody(lowStockParts);

            foreach (string email in adminEmails)
            {
                await emailService.SendAsync(email, "VPIMS Admin", subject, body, cancellationToken);
            }

            logger.LogInformation(
                "Low-stock alert sent to {AdminCount} admin(s) for {PartCount} part(s).",
                adminEmails.Count, lowStockParts.Count);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown — expected.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error in LowStockNotificationService.");
        }
    }

    private static string BuildLowStockEmailBody(IReadOnlyList<Part> parts)
    {
        var rows = string.Join("\n", parts.Select(p =>
            $"""
            <tr>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb;">{p.PartNumber}</td>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb;">{p.PartName}</td>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb;">{p.Category?.CategoryName ?? "—"}</td>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb;color:#dc2626;font-weight:bold;">{p.StockQuantity}</td>
              <td style="padding:6px 12px;border-bottom:1px solid #e5e7eb;">{p.ReorderLevel}</td>
            </tr>
            """));

        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:Arial,sans-serif;color:#111827;margin:0;padding:24px;">
              <h2 style="color:#dc2626;">⚠️ Low Stock Alert</h2>
              <p>The following {parts.Count} part(s) are at or below their reorder level and require restocking:</p>
              <table style="border-collapse:collapse;width:100%;max-width:700px;">
                <thead>
                  <tr style="background:#f3f4f6;">
                    <th style="padding:8px 12px;text-align:left;">Part #</th>
                    <th style="padding:8px 12px;text-align:left;">Part Name</th>
                    <th style="padding:8px 12px;text-align:left;">Category</th>
                    <th style="padding:8px 12px;text-align:left;">In Stock</th>
                    <th style="padding:8px 12px;text-align:left;">Reorder Level</th>
                  </tr>
                </thead>
                <tbody>
                  {rows}
                </tbody>
              </table>
              <p style="margin-top:24px;font-size:12px;color:#6b7280;">
                This is an automated message from VPIMS. Please log in to manage inventory.
              </p>
            </body>
            </html>
            """;
    }
}
