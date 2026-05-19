using Microsoft.Extensions.Options;
using Vpims.Application.Common;
using Vpims.Application.DTOs.Alerts;
using Vpims.Application.Interfaces;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Options;

namespace Vpims.Infrastructure.Services;

public sealed class AlertService(
    IPartRepository partRepository,
    ISalesRepository salesRepository,
    IPredictiveAlertRepository predictiveAlertRepository,
    IUserRepository userRepository,
    IEmailService emailService,
    IOptions<AlertConfigurationOptions> configuration) : IAlertService
{
    private readonly AlertConfigurationOptions alertConfiguration = NormalizeOptions(configuration.Value);

    public async Task<AlertSummaryResponse> GetAlertSummaryAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Part> allParts = await partRepository.GetAllAsync(cancellationToken);
        IReadOnlyList<Sale> overdueSales = await salesRepository.GetOverdueSalesAsync(alertConfiguration.OverdueCreditMonthsThreshold, DateTimeOffset.UtcNow, cancellationToken);
        IReadOnlyList<PredictiveAlert> predictiveAlerts = await predictiveAlertRepository.GetActiveAsync(cancellationToken);

        return BuildSummary(allParts, overdueSales, predictiveAlerts, DateTimeOffset.UtcNow);
    }

    public async Task<AlertSummaryResponse> GenerateAlertsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PredictiveAlert> predictiveAlerts = await predictiveAlertRepository.GetActiveAsync(cancellationToken);
        DateTimeOffset generatedAt = DateTimeOffset.UtcNow;

        foreach (PredictiveAlert existingAlert in predictiveAlerts.Where(alert => alert.Customer is not null && alert.Vehicle is not null))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (existingAlert.Vehicle is null)
            {
                continue;
            }

            string reminderMessage = $"Check vehicle {existingAlert.Vehicle.VehicleNumber} for upcoming maintenance.";
            bool exists = await predictiveAlertRepository.ExistsActiveAlertAsync(
                existingAlert.CustomerId,
                existingAlert.VehicleId,
                reminderMessage,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            await predictiveAlertRepository.AddAsync(new PredictiveAlert
            {
                CustomerId = existingAlert.CustomerId,
                VehicleId = existingAlert.VehicleId,
                PartId = existingAlert.PartId,
                AlertMessage = reminderMessage,
                RiskLevel = existingAlert.RiskLevel,
                Status = "Active",
                CreatedAt = generatedAt,
            }, cancellationToken);
        }

        IReadOnlyList<Part> allParts = await partRepository.GetAllAsync(cancellationToken);
    IReadOnlyList<Sale> overdueSales = await salesRepository.GetOverdueSalesAsync(alertConfiguration.OverdueCreditMonthsThreshold, generatedAt, cancellationToken);
        IReadOnlyList<PredictiveAlert> refreshedPredictiveAlerts = await predictiveAlertRepository.GetActiveAsync(cancellationToken);

        await SendLowStockAlertEmailsAsync(allParts, cancellationToken);
        await SendOverdueCreditReminderEmailsAsync(overdueSales, cancellationToken);

        return BuildSummary(allParts, overdueSales, refreshedPredictiveAlerts, generatedAt);
    }

    private async Task SendLowStockAlertEmailsAsync(IReadOnlyList<Part> allParts, CancellationToken cancellationToken)
    {
        IReadOnlyList<Part> lowStockParts = allParts
            .Where(part => part.StockQuantity < alertConfiguration.LowStockThreshold)
            .OrderBy(part => part.StockQuantity)
            .ThenBy(part => part.PartName)
            .ToList();

        if (lowStockParts.Count == 0)
        {
            return;
        }

        IReadOnlyList<User> adminUsers = await userRepository.GetUsersByRoleAsync(SystemRoles.Admin, cancellationToken);

        if (adminUsers.Count == 0)
        {
            return;
        }

        string tableRows = string.Join(string.Empty, lowStockParts.Select(part =>
            $"<tr><td style=\"padding:8px;border:1px solid #d0d7de;\">{part.PartName}</td><td style=\"padding:8px;border:1px solid #d0d7de;\">{part.PartNumber}</td><td style=\"padding:8px;border:1px solid #d0d7de;text-align:right;\">{part.StockQuantity}</td><td style=\"padding:8px;border:1px solid #d0d7de;text-align:right;\">{alertConfiguration.LowStockThreshold}</td></tr>"));
        string htmlBody = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;color:#1f2933;">
                <h2>Low-stock summary</h2>
                <p>The following parts are below the operational threshold of {alertConfiguration.LowStockThreshold} units.</p>
                <table style="border-collapse:collapse;width:100%;margin:16px 0;">
                    <thead>
                        <tr style="background:#f4f6f8;">
                            <th style="padding:8px;border:1px solid #d0d7de;text-align:left;">Part</th>
                            <th style="padding:8px;border:1px solid #d0d7de;text-align:left;">Part Number</th>
                            <th style="padding:8px;border:1px solid #d0d7de;text-align:right;">Stock</th>
                            <th style="padding:8px;border:1px solid #d0d7de;text-align:right;">Threshold</th>
                        </tr>
                    </thead>
                    <tbody>{tableRows}</tbody>
                </table>
            </div>
            """;

        foreach (User adminUser in adminUsers.Where(user => !string.IsNullOrWhiteSpace(user.Email)))
        {
            await TrySendEmailAsync(
                adminUser.Email!,
                $"Autonix low-stock alert: {lowStockParts.Count} item(s) below threshold",
                htmlBody,
                cancellationToken);
        }
    }

    private async Task SendOverdueCreditReminderEmailsAsync(IReadOnlyList<Sale> overdueSales, CancellationToken cancellationToken)
    {
        IReadOnlyList<IGrouping<int, Sale>> salesByCustomer = overdueSales
            .Where(sale => !string.IsNullOrWhiteSpace(sale.Customer?.Email))
            .GroupBy(sale => sale.CustomerId)
            .ToList();

        foreach (IGrouping<int, Sale> customerSales in salesByCustomer)
        {
            Sale firstSale = customerSales.First();
            string customerName = firstSale.Customer?.FullName ?? "Customer";
            string recipientEmail = firstSale.Customer?.Email ?? string.Empty;
            string tableRows = string.Join(string.Empty, customerSales.Select(sale =>
                $"<tr><td style=\"padding:8px;border:1px solid #d0d7de;\">{sale.InvoiceNumber}</td><td style=\"padding:8px;border:1px solid #d0d7de;text-align:right;\">{sale.TotalAmount:F2}</td><td style=\"padding:8px;border:1px solid #d0d7de;\">{sale.DueDate?.UtcDateTime:yyyy-MM-dd}</td><td style=\"padding:8px;border:1px solid #d0d7de;\">{sale.PaymentStatus}</td></tr>"));
            string htmlBody = $"""
                <div style="font-family:Segoe UI,Arial,sans-serif;color:#1f2933;">
                    <h2>Payment reminder</h2>
                    <p>Dear {customerName},</p>
                    <p>The following invoices are overdue. Please contact the service center if you need help settling them.</p>
                    <table style="border-collapse:collapse;width:100%;margin:16px 0;">
                        <thead>
                            <tr style="background:#f4f6f8;">
                                <th style="padding:8px;border:1px solid #d0d7de;text-align:left;">Invoice</th>
                                <th style="padding:8px;border:1px solid #d0d7de;text-align:right;">Amount</th>
                                <th style="padding:8px;border:1px solid #d0d7de;text-align:left;">Due date</th>
                                <th style="padding:8px;border:1px solid #d0d7de;text-align:left;">Status</th>
                            </tr>
                        </thead>
                        <tbody>{tableRows}</tbody>
                    </table>
                </div>
                """;

            await TrySendEmailAsync(recipientEmail, "Autonix overdue credit reminder", htmlBody, cancellationToken);
        }
    }

    private async Task TrySendEmailAsync(string recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        try
        {
            await emailService.SendEmailAsync(recipientEmail, subject, htmlBody, cancellationToken);
        }
        catch (AppValidationException)
        {
        }
    }

    private AlertSummaryResponse BuildSummary(
        IReadOnlyList<Part> allParts,
        IReadOnlyList<Sale> overdueSales,
        IReadOnlyList<PredictiveAlert> predictiveAlerts,
        DateTimeOffset generatedAt)
    {
        IReadOnlyList<LowStockAlertResponse> lowStockAlerts = allParts
            .Where(part => part.StockQuantity < alertConfiguration.LowStockThreshold)
            .OrderBy(part => part.StockQuantity)
            .ThenBy(part => part.PartName)
            .Take(alertConfiguration.DashboardAlertLimit)
            .Select(part => new LowStockAlertResponse
            {
                PartId = part.PartId,
                PartNumber = part.PartNumber,
                PartName = part.PartName,
                CategoryName = part.Category?.CategoryName,
                StockQuantity = part.StockQuantity,
                Threshold = alertConfiguration.LowStockThreshold,
            })
            .ToList();

        IReadOnlyList<OverdueCreditAlertResponse> overdueCreditAlerts = overdueSales
            .OrderByDescending(sale => sale.DueDate)
            .Take(alertConfiguration.DashboardAlertLimit)
            .Select(sale => new OverdueCreditAlertResponse
            {
                SaleId = sale.SaleId,
                InvoiceNumber = sale.InvoiceNumber,
                CustomerId = sale.CustomerId,
                CustomerName = sale.Customer?.FullName ?? "Unknown",
                CustomerEmail = sale.Customer?.Email,
                OutstandingAmount = sale.TotalAmount,
                PaymentStatus = sale.PaymentStatus,
                DueDate = sale.DueDate,
                DaysOverdue = sale.DueDate.HasValue
                    ? Math.Max(0, (int)(generatedAt.Date - sale.DueDate.Value.UtcDateTime.Date).TotalDays)
                    : 0,
            })
            .ToList();

        IReadOnlyList<PredictiveAlertResponse> predictiveAlertResponses = predictiveAlerts
            .Take(alertConfiguration.DashboardAlertLimit)
            .Select(alert => new PredictiveAlertResponse
            {
                PredictiveAlertId = alert.PredictiveAlertId,
                CustomerId = alert.CustomerId,
                CustomerName = alert.Customer?.FullName ?? "Unknown",
                VehicleId = alert.VehicleId,
                VehicleNumber = alert.Vehicle?.VehicleNumber ?? "Unknown",
                PartId = alert.PartId,
                PartName = alert.Part?.PartName,
                AlertMessage = alert.AlertMessage,
                RiskLevel = alert.RiskLevel,
                Status = alert.Status,
                CreatedAt = alert.CreatedAt,
            })
            .ToList();

        return new AlertSummaryResponse
        {
            ActiveAlertCount = lowStockAlerts.Count + overdueCreditAlerts.Count + predictiveAlertResponses.Count,
            LowStockAlertCount = lowStockAlerts.Count,
            OverdueCreditAlertCount = overdueCreditAlerts.Count,
            PredictiveAlertCount = predictiveAlertResponses.Count,
            GeneratedAt = generatedAt,
            LowStockAlerts = lowStockAlerts,
            OverdueCreditAlerts = overdueCreditAlerts,
            PredictiveAlerts = predictiveAlertResponses,
        };
    }

    private static AlertConfigurationOptions NormalizeOptions(AlertConfigurationOptions? options)
    {
        return new AlertConfigurationOptions
        {
            LowStockThreshold = Math.Max(0, options?.LowStockThreshold ?? 10),
            OverdueCreditMonthsThreshold = Math.Max(0, options?.OverdueCreditMonthsThreshold ?? 1),
            DashboardAlertLimit = Math.Max(1, options?.DashboardAlertLimit ?? 5),
        };
    }
}