using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vpims.Application.Common;
using Vpims.Application.Interfaces;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Data;
using Vpims.Infrastructure.Options;
using Vpims.Infrastructure.Persistence;
using Vpims.Infrastructure.Repositories;
using Vpims.Infrastructure.Security;
using Vpims.Infrastructure.Services;

namespace Vpims.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, string webRootPath)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<DatabaseInitializationOptions>(configuration.GetSection(DatabaseInitializationOptions.SectionName));
        services.Configure<AlertConfigurationOptions>(configuration.GetSection(AlertConfigurationOptions.SectionName));
        services.Configure<InvoiceEmailOptions>(configuration.GetSection(InvoiceEmailOptions.SectionName));
        services.Configure<PasswordResetOptions>(configuration.GetSection(PasswordResetOptions.SectionName));

        DatabaseInitializationOptions databaseOptions = configuration
            .GetSection(DatabaseInitializationOptions.SectionName)
            .Get<DatabaseInitializationOptions>()
            ?? new DatabaseInitializationOptions();

        string? connectionString = configuration.GetConnectionString("defaultConnection");

        services.AddDbContext<AppDbContext>(options => ConfigureAppDbContext(options, databaseOptions, connectionString));
        services.AddDbContext<VpimsDbContext>(options => ConfigureStaffSalesDbContext(options, databaseOptions, connectionString));

        services.AddScoped<PasswordHasher<User>>();
        services.AddScoped<DatabaseSchemaCompatibilityEvaluator>();
        services.AddScoped<DatabaseLocalDataSafetyEvaluator>();
        services.AddScoped<DatabaseInitializationPlanner>();
        services.AddScoped<IDatabaseLifecycleManager>(serviceProvider =>
        {
            DatabaseInitializationOptions configuredOptions = serviceProvider
                .GetRequiredService<IOptions<DatabaseInitializationOptions>>()
                .Value;

            if (string.Equals(configuredOptions.Provider, DatabaseInitializationOptions.InMemoryProvider, StringComparison.OrdinalIgnoreCase))
            {
                return new InMemoryDatabaseLifecycleManager();
            }

            if (!string.Equals(configuredOptions.Provider, DatabaseInitializationOptions.PostgreSqlProvider, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unsupported database provider '{configuredOptions.Provider}'.");
            }

            return ActivatorUtilities.CreateInstance<PostgreSqlDatabaseLifecycleManager>(serviceProvider);
        });
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<VpimsDbSeeder>();
        services.AddScoped<DatabaseInitializer>();

        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerReportRepository, CustomerReportRepository>();
        services.AddScoped<IFinancialReportRepository, FinancialReportRepository>();
        services.AddScoped<IPredictiveAlertRepository, PredictiveAlertRepository>();
        services.AddScoped<IPartRepository, PartRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IPartRequestRepository, PartRequestRepository>();
        services.AddScoped<IServiceReviewRepository, ServiceReviewRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IPurchaseInvoiceRepository, PurchaseInvoiceRepository>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IAiVehicleInsightsService, RuleBasedAiVehicleInsightsService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICustomerReportService, CustomerReportService>();
        services.AddScoped<IDevEmailService, DevEmailService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IFinancialReportService, FinancialReportService>();
        services.AddScoped<IPartImageStorage>(_ => new LocalPartImageStorage(Path.Combine(webRootPath, "uploads", "parts")));
        services.AddScoped<IStaffManagementService, StaffManagementService>();
        services.AddScoped<IPartService, PartService>();
        services.AddScoped<IPartRequestService, PartRequestService>();
        services.AddScoped<IServiceReviewService, ServiceReviewService>();

        services.AddScoped<ISalesRepository, SaleRepository>();
        services.AddScoped<ISaleService, SalesService>();

        return services;
    }

    private static void ConfigureAppDbContext(
        DbContextOptionsBuilder options,
        DatabaseInitializationOptions databaseOptions,
        string? connectionString)
    {
        if (string.Equals(databaseOptions.Provider, DatabaseInitializationOptions.InMemoryProvider, StringComparison.OrdinalIgnoreCase))
        {
            options.UseInMemoryDatabase(BuildInMemoryDatabaseName(databaseOptions.InMemoryDatabaseName, "app"));
            return;
        }

        options.UseNpgsql(RequireConnectionString(connectionString, databaseOptions.Provider));
    }

    private static void ConfigureStaffSalesDbContext(
        DbContextOptionsBuilder options,
        DatabaseInitializationOptions databaseOptions,
        string? connectionString)
    {
        if (string.Equals(databaseOptions.Provider, DatabaseInitializationOptions.InMemoryProvider, StringComparison.OrdinalIgnoreCase))
        {
            options.UseInMemoryDatabase(BuildInMemoryDatabaseName(databaseOptions.InMemoryDatabaseName, "staff-sales"));
            return;
        }

        options.UseNpgsql(RequireConnectionString(connectionString, databaseOptions.Provider));
    }

    private static string RequireConnectionString(string? connectionString, string provider)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string 'defaultConnection' is required when using the {provider} provider.");
        }

        return connectionString;
    }

    private static string BuildInMemoryDatabaseName(string baseName, string suffix)
    {
        return $"{baseName}-{suffix}";
    }
}