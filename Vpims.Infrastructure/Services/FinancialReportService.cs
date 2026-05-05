using Vpims.Application.DTOs.Reports;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class FinancialReportService(IFinancialReportRepository financialReportRepository) : IFinancialReportService
{
    public async Task<FinancialReportResponse> GetDailyReportAsync(
        DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        DateOnly targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        DateTimeOffset rangeStart = ToUtcStartOfDay(targetDate);
        DateTimeOffset rangeEndExclusive = rangeStart.AddDays(1);

        IReadOnlyList<Sale> sales = await financialReportRepository.GetSalesInRangeAsync(rangeStart, rangeEndExclusive, cancellationToken);
        IReadOnlyList<PurchaseInvoice> purchaseInvoices = await financialReportRepository.GetPurchaseInvoicesInRangeAsync(rangeStart, rangeEndExclusive, cancellationToken);

        return BuildReport(
            reportType: "Daily",
            periodLabel: targetDate.ToString("yyyy-MM-dd"),
            rangeStart,
            rangeEndExclusive,
            sales,
            purchaseInvoices,
            bucketLabelFactory: timestamp => timestamp.UtcDateTime.ToString("yyyy-MM-dd"));
    }

    public async Task<FinancialReportResponse> GetMonthlyReportAsync(
        int? year = null,
        int? month = null,
        CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        int resolvedYear = year ?? now.Year;
        int resolvedMonth = month ?? now.Month;

        DateTimeOffset rangeStart = new(new DateTime(resolvedYear, resolvedMonth, 1, 0, 0, 0, DateTimeKind.Utc));
        DateTimeOffset rangeEndExclusive = rangeStart.AddMonths(1);

        IReadOnlyList<Sale> sales = await financialReportRepository.GetSalesInRangeAsync(rangeStart, rangeEndExclusive, cancellationToken);
        IReadOnlyList<PurchaseInvoice> purchaseInvoices = await financialReportRepository.GetPurchaseInvoicesInRangeAsync(rangeStart, rangeEndExclusive, cancellationToken);

        return BuildReport(
            reportType: "Monthly",
            periodLabel: $"{resolvedYear:D4}-{resolvedMonth:D2}",
            rangeStart,
            rangeEndExclusive,
            sales,
            purchaseInvoices,
            bucketLabelFactory: timestamp => timestamp.UtcDateTime.ToString("yyyy-MM-dd"));
    }

    public async Task<FinancialReportResponse> GetYearlyReportAsync(
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        int resolvedYear = year ?? DateTime.UtcNow.Year;
        DateTimeOffset rangeStart = new(new DateTime(resolvedYear, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        DateTimeOffset rangeEndExclusive = rangeStart.AddYears(1);

        IReadOnlyList<Sale> sales = await financialReportRepository.GetSalesInRangeAsync(rangeStart, rangeEndExclusive, cancellationToken);
        IReadOnlyList<PurchaseInvoice> purchaseInvoices = await financialReportRepository.GetPurchaseInvoicesInRangeAsync(rangeStart, rangeEndExclusive, cancellationToken);

        return BuildReport(
            reportType: "Yearly",
            periodLabel: resolvedYear.ToString("D4"),
            rangeStart,
            rangeEndExclusive,
            sales,
            purchaseInvoices,
            bucketLabelFactory: timestamp => timestamp.UtcDateTime.ToString("yyyy-MM"));
    }

    private static FinancialReportResponse BuildReport(
        string reportType,
        string periodLabel,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive,
        IReadOnlyList<Sale> sales,
        IReadOnlyList<PurchaseInvoice> purchaseInvoices,
        Func<DateTimeOffset, string> bucketLabelFactory)
    {
        IReadOnlyList<PurchaseInvoice> includedInvoices = purchaseInvoices
            .Where(invoice => string.IsNullOrWhiteSpace(invoice.Status)
                || string.Equals(invoice.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .ToList();

        decimal revenue = sales.Sum(sale => sale.TotalAmount);
        decimal discounts = sales.Sum(sale => sale.DiscountAmount);
        decimal purchaseCosts = includedInvoices.Sum(invoice => invoice.TotalAmount);

        var entries = sales
            .GroupBy(sale => bucketLabelFactory(sale.SaleDate))
            .ToDictionary(
                group => group.Key,
                group => new FinancialReportEntryResponse
                {
                    Label = group.Key,
                    Revenue = group.Sum(sale => sale.TotalAmount),
                    Discounts = group.Sum(sale => sale.DiscountAmount),
                    SaleCount = group.Count()
                });

        foreach (IGrouping<string, PurchaseInvoice> invoiceGroup in includedInvoices.GroupBy(invoice => bucketLabelFactory(invoice.InvoiceDate)))
        {
            if (!entries.TryGetValue(invoiceGroup.Key, out FinancialReportEntryResponse? entry))
            {
                entry = new FinancialReportEntryResponse
                {
                    Label = invoiceGroup.Key,
                };
                entries[invoiceGroup.Key] = entry;
            }

            entry.PurchaseCosts = invoiceGroup.Sum(invoice => invoice.TotalAmount);
            entry.PurchaseInvoiceCount = invoiceGroup.Count();
        }

        IReadOnlyList<FinancialReportEntryResponse> orderedEntries = entries.Values
            .Select(entry =>
            {
                entry.GrossProfit = entry.Revenue - entry.PurchaseCosts;
                return entry;
            })
            .OrderBy(entry => entry.Label)
            .ToList();

        return new FinancialReportResponse
        {
            ReportType = reportType,
            PeriodLabel = periodLabel,
            RangeStart = rangeStart,
            RangeEndExclusive = rangeEndExclusive,
            Revenue = revenue,
            Discounts = discounts,
            PurchaseCosts = purchaseCosts,
            GrossProfit = revenue - purchaseCosts,
            SaleCount = sales.Count,
            PurchaseInvoiceCount = includedInvoices.Count,
            Entries = orderedEntries,
        };
    }

    private static DateTimeOffset ToUtcStartOfDay(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
    }
}