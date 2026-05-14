using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vpims.Application.Interfaces;
using Vpims.Application.DTOs.Alerts;
using Vpims.Application.DTOs.Appointments;
using Vpims.Application.DTOs.Auth;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;
using Vpims.Infrastructure.Repositories;
using Vpims.Infrastructure.Services;
using Xunit;

namespace Vpims.Member4.Backend.Tests;

public sealed class OperationalCoverageTests
{
    [Fact]
    public async Task GetAlertSummaryAsync_ReturnsLowStockOverdueAndPredictiveSignals()
    {
        await using var harness = await TestHarness.CreateAsync();
        SeedCustomerProfile(harness.DbContext, customerId: 1, userId: 10, fullName: "Nina Driver", vehicleId: 5, vehicleNumber: "BA-2-CHA-2222");

        harness.DbContext.Parts.AddRange(
            new Part
            {
                PartId = 11,
                PartNumber = "BP-11",
                PartName = "Brake Pad",
                StockQuantity = 3,
                ReorderLevel = 5,
                CostPrice = 90m,
                UnitPrice = 140m,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            new Part
            {
                PartId = 12,
                PartNumber = "OF-12",
                PartName = "Oil Filter",
                StockQuantity = 25,
                ReorderLevel = 10,
                CostPrice = 15m,
                UnitPrice = 30m,
                CreatedAt = DateTimeOffset.UtcNow,
            });

        harness.DbContext.Sales.Add(new Sale
        {
            SaleId = 21,
            CustomerId = 1,
            CreatedByUserId = 10,
            InvoiceNumber = "SAL-OVERDUE-001",
            Subtotal = 1200m,
            DiscountAmount = 0m,
            TotalAmount = 1200m,
            PaymentStatus = "Credit",
            DueDate = DateTimeOffset.UtcNow.AddMonths(-2),
            SaleDate = DateTimeOffset.UtcNow.AddMonths(-3),
        });

        harness.DbContext.PredictiveAlerts.Add(new PredictiveAlert
        {
            PredictiveAlertId = 31,
            CustomerId = 1,
            VehicleId = 5,
            AlertMessage = "Replace brake pad soon.",
            RiskLevel = "High",
            Status = "Active",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
        });

        await harness.DbContext.SaveChangesAsync();

        AlertSummaryResponse summary = await harness.AlertService.GetAlertSummaryAsync();

        Assert.Equal(3, summary.ActiveAlertCount);
        Assert.Equal(1, summary.LowStockAlertCount);
        Assert.Equal(1, summary.OverdueCreditAlertCount);
        Assert.Equal(1, summary.PredictiveAlertCount);
        Assert.Equal("Brake Pad", Assert.Single(summary.LowStockAlerts).PartName);
        Assert.Equal("Nina Driver", Assert.Single(summary.OverdueCreditAlerts).CustomerName);
        Assert.Equal("Replace brake pad soon.", Assert.Single(summary.PredictiveAlerts).AlertMessage);
    }

    [Fact]
    public async Task GetReportAsync_ClassifiesRegularHighSpenderAndPendingCreditCustomers()
    {
        await using var harness = await TestHarness.CreateAsync();
        SeedCustomerProfile(harness.DbContext, customerId: 1, userId: 10, fullName: "Nina Driver", vehicleId: 5, vehicleNumber: "BA-2-CHA-2222");
        SeedCustomerProfile(harness.DbContext, customerId: 2, userId: 11, fullName: "Ava Walker", vehicleId: 6, vehicleNumber: "BA-3-CHA-3333");

        harness.DbContext.Sales.AddRange(
            new Sale
            {
                SaleId = 41,
                CustomerId = 1,
                CreatedByUserId = 10,
                InvoiceNumber = "SAL-REGULAR-001",
                Subtotal = 6200m,
                DiscountAmount = 200m,
                TotalAmount = 6000m,
                PaymentStatus = "Paid",
                SaleDate = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero),
            },
            new Sale
            {
                SaleId = 42,
                CustomerId = 2,
                CreatedByUserId = 11,
                InvoiceNumber = "SAL-CREDIT-001",
                Subtotal = 1000m,
                DiscountAmount = 0m,
                TotalAmount = 1000m,
                PaymentStatus = "Pending",
                DueDate = DateTimeOffset.UtcNow.AddMonths(-2),
                SaleDate = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero),
            });

        harness.DbContext.Appointments.AddRange(
            new Appointment
            {
                AppointmentId = 51,
                CustomerId = 1,
                VehicleId = 5,
                AppointmentDate = new DateTimeOffset(2026, 5, 11, 9, 0, 0, TimeSpan.Zero),
                ServiceType = "Brake inspection",
                Status = "Completed",
                CreatedAt = new DateTimeOffset(2026, 5, 1, 9, 0, 0, TimeSpan.Zero),
            },
            new Appointment
            {
                AppointmentId = 52,
                CustomerId = 2,
                VehicleId = 6,
                AppointmentDate = new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero),
                ServiceType = "Oil change",
                Status = "Confirmed",
                CreatedAt = new DateTimeOffset(2026, 5, 2, 9, 0, 0, TimeSpan.Zero),
            });

        await harness.DbContext.SaveChangesAsync();

        var report = await harness.CustomerReportService.GetReportAsync(
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 15),
            5000m);

        Assert.Equal(2, report.RegularCustomerCount);
        Assert.Single(report.HighSpenders);
        Assert.Equal("Nina Driver", report.HighSpenders[0].FullName);
        Assert.Single(report.PendingCredits);
        Assert.Equal("Ava Walker", report.PendingCredits[0].FullName);
        Assert.Equal(1, report.OverdueCreditCustomerCount);
    }

    [Fact]
    public async Task CreateAppointmentAsync_NormalizesAppointmentDateToUtc()
    {
        await using var harness = await TestHarness.CreateAsync();
        SeedCustomerProfile(harness.DbContext, customerId: 1, userId: 10, fullName: "Nina Driver", vehicleId: 5, vehicleNumber: "BA-2-CHA-2222");
        await harness.DbContext.SaveChangesAsync();

        UserProfileResponse currentUser = new()
        {
            UserId = 10,
            CustomerId = 1,
            FullName = "Nina Driver",
            Email = "nina@example.com",
            PhoneNumber = "+9779800000001",
            Role = SystemRoles.Customer,
            IsActive = true,
        };

        AppointmentResponse response = await harness.AppointmentService.CreateAppointmentAsync(currentUser, new CreateAppointmentRequest
        {
            VehicleId = 5,
            AppointmentDate = new DateTimeOffset(2026, 5, 20, 14, 30, 0, TimeSpan.FromHours(5.75)),
            ServiceType = "Engine diagnostics",
            Notes = "Intermittent warning light",
        });

        Appointment storedAppointment = await harness.DbContext.Appointments.SingleAsync();

        Assert.Equal(TimeSpan.Zero, response.AppointmentDate.Offset);
        Assert.Equal(TimeSpan.Zero, storedAppointment.AppointmentDate.Offset);
        Assert.Equal("Pending", storedAppointment.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_PersistsUpdatedAppointmentStatus()
    {
        await using var harness = await TestHarness.CreateAsync();
        SeedCustomerProfile(harness.DbContext, customerId: 1, userId: 10, fullName: "Nina Driver", vehicleId: 5, vehicleNumber: "BA-2-CHA-2222");

        harness.DbContext.Appointments.Add(new Appointment
        {
            AppointmentId = 60,
            CustomerId = 1,
            VehicleId = 5,
            AppointmentDate = DateTimeOffset.UtcNow.AddDays(2),
            ServiceType = "Tyre rotation",
            Status = "Pending",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await harness.DbContext.SaveChangesAsync();

        AppointmentResponse response = await harness.AppointmentService.UpdateStatusAsync(60, new UpdateAppointmentStatusRequest
        {
            Status = "Completed",
        });

        Appointment storedAppointment = await harness.DbContext.Appointments.SingleAsync();

        Assert.Equal("Completed", response.Status);
        Assert.Equal("Completed", storedAppointment.Status);
    }

    private static void SeedCustomerProfile(AppDbContext dbContext, int customerId, int userId, string fullName, int vehicleId, string vehicleNumber)
    {
        dbContext.Users.Add(new User
        {
            UserId = userId,
            RoleId = 3,
            FullName = fullName,
            Email = $"{fullName.Replace(" ", ".").ToLowerInvariant()}@autonix.local",
            PhoneNumber = $"+977980000{userId:D4}",
            PasswordHash = "hashed",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        dbContext.Customers.Add(new Customer
        {
            CustomerId = customerId,
            UserId = userId,
            FullName = fullName,
            Email = $"{fullName.Replace(" ", ".").ToLowerInvariant()}@autonix.local",
            PhoneNumber = $"+977980000{userId:D4}",
            RegisteredAt = DateTimeOffset.UtcNow,
        });

        dbContext.Vehicles.Add(new Vehicle
        {
            VehicleId = vehicleId,
            CustomerId = customerId,
            VehicleNumber = vehicleNumber,
            Model = "Civic",
        });
    }

    private sealed class TestHarness(AppDbContext dbContext) : IAsyncDisposable
    {
        public AppDbContext DbContext { get; } = dbContext;

        public AlertService AlertService { get; } = new(
            new PartRepository(dbContext),
            new SaleRepository(dbContext),
            new PredictiveAlertRepository(dbContext),
            new UserRepository(dbContext),
            new NoOpEmailService());

        public CustomerReportService CustomerReportService { get; } = new(new CustomerReportRepository(dbContext));

        public AppointmentService AppointmentService { get; } = new(
            new AppointmentRepository(dbContext),
            new CustomerRepository(dbContext),
            new ServiceReviewRepository(dbContext));

        public static async Task<TestHarness> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var dbContext = new AppDbContext(options);

            dbContext.Roles.AddRange(
                new Role { RoleId = 1, Name = SystemRoles.Admin, Description = "Admin" },
                new Role { RoleId = 2, Name = SystemRoles.Staff, Description = "Staff" },
                new Role { RoleId = 3, Name = SystemRoles.Customer, Description = "Customer" });

            await dbContext.SaveChangesAsync();
            return new TestHarness(dbContext);
        }

        public ValueTask DisposeAsync()
        {
            return DbContext.DisposeAsync();
        }
    }

    private sealed class NoOpEmailService : IEmailService
    {
        public Task SendEmailAsync(string recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
