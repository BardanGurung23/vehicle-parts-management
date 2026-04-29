using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vpims.Application.Common;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;
using Vpims.Infrastructure.Repositories;
using Vpims.Infrastructure.Security;
using Vpims.Infrastructure.Services;

namespace Vpims.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<DatabaseInitializationOptions>(configuration.GetSection(DatabaseInitializationOptions.SectionName));

        string connectionString = configuration.GetConnectionString("defaultConnection")
            ?? throw new InvalidOperationException("Connection string 'defaultConnection' is missing.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<PasswordHasher<User>>();
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<DatabaseInitializer>();

        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPartRepository, PartRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IPartRequestRepository, PartRequestRepository>();
        services.AddScoped<IServiceReviewRepository, ServiceReviewRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IStaffManagementService, StaffManagementService>();
        services.AddScoped<IPartService, PartService>();
        services.AddScoped<IPartRequestService, PartRequestService>();
        services.AddScoped<IServiceReviewService, ServiceReviewService>();

        services.AddScoped<ISalesRepository, SaleRepository>();
        services.AddScoped<ISaleService, SalesService>();

        return services;
    }
}