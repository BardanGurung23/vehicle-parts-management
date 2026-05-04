namespace Vpims.Application.Common;

public sealed class NotificationSettings
{
    public const string SectionName = "Notifications";

    /// <summary>How often (in minutes) to check for low-stock parts.</summary>
    public int LowStockCheckIntervalMinutes { get; set; } = 60;

    /// <summary>How often (in minutes) to check for overdue unpaid invoices.</summary>
    public int UnpaidInvoiceCheckIntervalMinutes { get; set; } = 720;
}
