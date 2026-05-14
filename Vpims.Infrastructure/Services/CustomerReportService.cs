using Vpims.Application.DTOs.Reports;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class CustomerReportService(ICustomerReportRepository customerReportRepository) : ICustomerReportService
{
    private const decimal DefaultHighSpenderThreshold = 5000m;

    public async Task<CustomerReportsResponse> GetReportAsync(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        decimal? highSpenderThreshold = null,
        CancellationToken cancellationToken = default)
    {
        DateOnly resolvedEndDate = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        DateOnly resolvedStartDate = startDate ?? resolvedEndDate.AddDays(-30);
        decimal resolvedThreshold = highSpenderThreshold ?? DefaultHighSpenderThreshold;

        DateTimeOffset rangeStart = ToUtcStartOfDay(resolvedStartDate);
        DateTimeOffset rangeEndExclusive = ToUtcStartOfDay(resolvedEndDate);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset overdueThreshold = now.AddMonths(-1);

        IReadOnlyList<Sale> sales = await customerReportRepository.GetSalesInRangeAsync(rangeStart, rangeEndExclusive, cancellationToken);
        IReadOnlyList<Appointment> appointments = await customerReportRepository.GetAppointmentsInRangeAsync(rangeStart, rangeEndExclusive, cancellationToken);
        IReadOnlyList<Sale> pendingCreditSales = await customerReportRepository.GetPendingCreditSalesAsync(cancellationToken);

        Dictionary<int, CustomerAccumulator> regularCustomerMap = [];
        Dictionary<int, CustomerAccumulator> pendingCreditCustomerMap = [];

        foreach (Sale sale in sales)
        {
            CustomerAccumulator accumulator = GetOrCreateAccumulator(regularCustomerMap, sale.CustomerId, sale.Customer);
            accumulator.TotalSpent += sale.TotalAmount;
            accumulator.SaleCount += 1;
            accumulator.LastActivityAt = Max(accumulator.LastActivityAt, sale.SaleDate);
        }

        foreach (Appointment appointment in appointments)
        {
            CustomerAccumulator accumulator = GetOrCreateAccumulator(regularCustomerMap, appointment.CustomerId, appointment.Customer);
            accumulator.AppointmentCount += 1;
            accumulator.LastActivityAt = Max(accumulator.LastActivityAt, appointment.AppointmentDate);
        }

        foreach (Sale sale in pendingCreditSales)
        {
            CustomerAccumulator accumulator = GetOrCreateAccumulator(pendingCreditCustomerMap, sale.CustomerId, sale.Customer);
            accumulator.TotalSpent += sale.TotalAmount;
            accumulator.SaleCount += 1;
            accumulator.PendingInvoiceCount += 1;
            accumulator.OutstandingAmount += sale.TotalAmount;
            accumulator.LastActivityAt = Max(accumulator.LastActivityAt, sale.SaleDate);

            if (sale.DueDate.HasValue && sale.DueDate.Value <= overdueThreshold)
            {
                accumulator.OverdueInvoiceCount += 1;
            }
        }

        IReadOnlyList<CustomerReportEntryResponse> regularCustomers = regularCustomerMap.Values
            .Select(ToResponse)
            .OrderByDescending(customer => customer.LastActivityAt)
            .ThenBy(customer => customer.FullName)
            .ToList();

        IReadOnlyList<CustomerReportEntryResponse> highSpenders = regularCustomers
            .Where(customer => customer.TotalSpent >= resolvedThreshold)
            .OrderByDescending(customer => customer.TotalSpent)
            .ThenBy(customer => customer.FullName)
            .ToList();

        IReadOnlyList<CustomerReportEntryResponse> pendingCredits = pendingCreditCustomerMap.Values
            .Select(ToResponse)
            .OrderByDescending(customer => customer.OverdueInvoiceCount)
            .ThenByDescending(customer => customer.OutstandingAmount)
            .ThenBy(customer => customer.FullName)
            .ToList();

        return new CustomerReportsResponse
        {
            PeriodLabel = $"{resolvedStartDate:yyyy-MM-dd} to {resolvedEndDate.AddDays(-1):yyyy-MM-dd}",
            RangeStart = rangeStart,
            RangeEndExclusive = rangeEndExclusive,
            HighSpenderThreshold = resolvedThreshold,
            RegularCustomerCount = regularCustomers.Count,
            HighSpenderCount = highSpenders.Count,
            PendingCreditCustomerCount = pendingCredits.Count,
            OverdueCreditCustomerCount = pendingCredits.Count(customer => customer.OverdueInvoiceCount > 0),
            RegularCustomers = regularCustomers,
            HighSpenders = highSpenders,
            PendingCredits = pendingCredits,
        };
    }

    private static CustomerAccumulator GetOrCreateAccumulator(
        IDictionary<int, CustomerAccumulator> customers,
        int customerId,
        Customer? customer)
    {
        if (customers.TryGetValue(customerId, out CustomerAccumulator? existing))
        {
            return existing;
        }

        var created = new CustomerAccumulator
        {
            CustomerId = customerId,
            FullName = customer?.FullName ?? "Unknown",
            PhoneNumber = customer?.PhoneNumber ?? string.Empty,
            Email = customer?.Email,
        };

        customers[customerId] = created;
        return created;
    }

    private static CustomerReportEntryResponse ToResponse(CustomerAccumulator accumulator)
    {
        return new CustomerReportEntryResponse
        {
            CustomerId = accumulator.CustomerId,
            FullName = accumulator.FullName,
            PhoneNumber = accumulator.PhoneNumber,
            Email = accumulator.Email,
            TotalSpent = accumulator.TotalSpent,
            SaleCount = accumulator.SaleCount,
            AppointmentCount = accumulator.AppointmentCount,
            PendingInvoiceCount = accumulator.PendingInvoiceCount,
            OverdueInvoiceCount = accumulator.OverdueInvoiceCount,
            OutstandingAmount = accumulator.OutstandingAmount,
            LastActivityAt = accumulator.LastActivityAt,
        };
    }

    private static DateTimeOffset ToUtcStartOfDay(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
    }

    private static DateTimeOffset? Max(DateTimeOffset? current, DateTimeOffset candidate)
    {
        if (!current.HasValue || candidate > current.Value)
        {
            return candidate;
        }

        return current.Value;
    }

    private sealed class CustomerAccumulator
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
}