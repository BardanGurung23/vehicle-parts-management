namespace Vpims.Infrastructure.Options;

public sealed class AlertConfigurationOptions
{
    public const string SectionName = "AlertConfiguration";

    public int LowStockThreshold { get; set; } = 10;

    public int OverdueCreditMonthsThreshold { get; set; } = 1;

    public int DashboardAlertLimit { get; set; } = 5;
}