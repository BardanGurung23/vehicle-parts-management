namespace Vpims.Application.DTOs.Reports;

public sealed class FinancialReportEntryResponse
{
    public string Label { get; set; } = string.Empty;

    public decimal Revenue { get; set; }

    public decimal Discounts { get; set; }

    public decimal PurchaseCosts { get; set; }

    public decimal GrossProfit { get; set; }

    public int SaleCount { get; set; }

    public int PurchaseInvoiceCount { get; set; }
}

public sealed class FinancialReportResponse
{
    public string ReportType { get; set; } = string.Empty;

    public string PeriodLabel { get; set; } = string.Empty;

    public DateTimeOffset RangeStart { get; set; }

    public DateTimeOffset RangeEndExclusive { get; set; }

    public decimal Revenue { get; set; }

    public decimal Discounts { get; set; }

    public decimal PurchaseCosts { get; set; }

    public decimal GrossProfit { get; set; }

    public int SaleCount { get; set; }

    public int PurchaseInvoiceCount { get; set; }

    public IReadOnlyList<FinancialReportEntryResponse> Entries { get; set; } = Array.Empty<FinancialReportEntryResponse>();
}