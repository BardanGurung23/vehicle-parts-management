namespace Vpims.Application.DTOs.Reports;

public sealed class CustomerReportEntryResponse
{
    public int CustomerId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public decimal TotalSpent { get; set; }

    public int SaleCount { get; set; }

    public int AppointmentCount { get; set; }

    public int PendingInvoiceCount { get; set; }

    public int OverdueInvoiceCount { get; set; }

    public decimal OutstandingAmount { get; set; }

    public DateTimeOffset? LastActivityAt { get; set; }
}

public sealed class CustomerReportsResponse
{
    public string ReportType { get; set; } = "Customer";

    public string PeriodLabel { get; set; } = string.Empty;

    public DateTimeOffset RangeStart { get; set; }

    public DateTimeOffset RangeEndExclusive { get; set; }

    public decimal HighSpenderThreshold { get; set; }

    public int RegularCustomerCount { get; set; }

    public int HighSpenderCount { get; set; }

    public int PendingCreditCustomerCount { get; set; }

    public int OverdueCreditCustomerCount { get; set; }

    public IReadOnlyList<CustomerReportEntryResponse> RegularCustomers { get; set; } = [];

    public IReadOnlyList<CustomerReportEntryResponse> HighSpenders { get; set; } = [];

    public IReadOnlyList<CustomerReportEntryResponse> PendingCredits { get; set; } = [];
}