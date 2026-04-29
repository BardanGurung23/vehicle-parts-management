using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vpims.Application.DTOs.Customers;
using Vpims.Application.DTOs.Users;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;
using Vpims.Infrastructure;
using Vpims.Infrastructure.Persistence;

string apiDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Vpims.API"));

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile(Path.Combine(apiDirectory, "appsettings.json"), optional: false)
    .AddJsonFile(Path.Combine(apiDirectory, "appsettings.Development.json"), optional: true)
    .AddEnvironmentVariables();

builder.Services.AddInfrastructureServices(builder.Configuration);

using IHost host = builder.Build();
using IServiceScope scope = host.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
var roleRepository = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
var staffManagementService = scope.ServiceProvider.GetRequiredService<IStaffManagementService>();
var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();

Console.WriteLine("=== Seeding Database ===");
Console.WriteLine();

Console.WriteLine("1. Seeding Roles...");
await SeedRolesAsync(dbContext);

Console.WriteLine("2. Seeding Part Categories...");
await SeedPartCategoriesAsync(dbContext);

Console.WriteLine("3. Seeding Demo Users...");
await SeedDemoUsersAsync(dbContext, passwordHasher, roleRepository, customerService, staffManagementService);

Console.WriteLine();
Console.WriteLine("=== Seeding Complete ===");
Console.WriteLine();
Console.WriteLine("Demo Accounts:");
Console.WriteLine("  Admin:  demo.admin@vpims.com  | Password: Admin123!");
Console.WriteLine("  Staff: demo.staff@vpims.com  | Password: Staff123!");
Console.WriteLine("  Customer: demo.customer@vpims.com | Password: Customer123!");

static async Task SeedRolesAsync(AppDbContext dbContext)
{
    string[] roles =
    [
        "Admin",
        "Staff",
        "Customer"
    ];

    string[] descriptions =
    [
        "System administrator with full access",
        "Staff user handling customers, sales, and invoices",
        "Customer self-service account"
    ];

    for (int i = 0; i < roles.Length; i++)
    {
        bool exists = await dbContext.Roles.AnyAsync(r => r.Name == roles[i]);

        if (!exists)
        {
            dbContext.Roles.Add(new Role
            {
                Name = roles[i],
                Description = descriptions[i]
            });
            Console.WriteLine($"   - Created role: {roles[i]}");
        }
        else
        {
            Console.WriteLine($"   - Role already exists: {roles[i]}");
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedPartCategoriesAsync(AppDbContext dbContext)
{
    var categories = new (string Name, string Description)[]
    {
        ("Engine", "Filters, belts, sensors, and engine service parts."),
        ("Brakes", "Pads, discs, cylinders, and brake hardware."),
        ("Suspension", "Shocks, bushings, arms, and alignment parts."),
        ("Electrical", "Batteries, lights, relays, and charging parts."),
        ("Body Parts", "Mirrors, lights, bumpers, and body panels."),
        ("Transmission", "Clutch, gear box, and drivetrain parts."),
        ("Oil & Fluids", "Engine oil, brake fluid, coolant, and filters."),
        ("Tyres & Wheels", "Tyres, rims, wheel bearings, and balancing.")
    };

    foreach (var (name, description) in categories)
    {
        bool exists = await dbContext.PartCategories.AnyAsync(c => c.CategoryName == name);

        if (!exists)
        {
            dbContext.PartCategories.Add(new PartCategory
            {
                CategoryName = name,
                Description = description
            });
            Console.WriteLine($"   - Created category: {name}");
        }
        else
        {
            Console.WriteLine($"   - Category already exists: {name}");
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task SeedDemoUsersAsync(
    AppDbContext dbContext,
    PasswordHasher<User> passwordHasher,
    IRoleRepository roleRepository,
    ICustomerService customerService,
    IStaffManagementService staffManagementService)
{
    var adminRole = await roleRepository.GetByNameAsync("Admin");
    var staffRole = await roleRepository.GetByNameAsync("Staff");

    if (adminRole is null || staffRole is null)
    {
        Console.WriteLine("   ERROR: Admin or Staff role not found!");
        return;
    }

    var demoUsers = new List<DemoUserInfo>
    {
        new() { FullName = "Demo Admin", Email = "demo.admin@vpims.com", Phone = "9800000001", Password = "Admin123!", RoleId = adminRole!.RoleId },
        new() { FullName = "Demo Staff", Email = "demo.staff@vpims.com", Phone = "9800000002", Password = "Staff123!", RoleId = staffRole!.RoleId },
        new() { FullName = "Demo Customer", Email = "demo.customer@vpims.com", Phone = "9800000003", Password = "Customer123!", Address = "Kathmandu, Nepal" }
    };

    foreach (var user in demoUsers)
    {
        var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);

        if (existingUser is not null)
        {
            Console.WriteLine($"   - Updating existing user: {user.Email}");
            existingUser.FullName = user.FullName;
            existingUser.PhoneNumber = user.Phone;
            existingUser.PasswordHash = passwordHasher.HashPassword(existingUser, user.Password);
            existingUser.IsActive = true;
            await dbContext.SaveChangesAsync();
            continue;
        }

        if (user.Address is not null)
        {
            var result = await customerService.RegisterAsync(new RegisterCustomerRequest
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.Phone,
                Password = user.Password,
                Address = user.Address
            });
            Console.WriteLine($"   - Created customer: {user.Email}");
        }
        else
        {
            var result = await staffManagementService.RegisterStaffAsync(new CreateStaffUserRequest
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.Phone,
                Password = user.Password,
                RoleId = user.RoleId ?? throw new InvalidOperationException("RoleId is required for staff.")
            });
            Console.WriteLine($"   - Created {result.Role.ToLowerInvariant()}: {user.Email}");
        }
    }

    await dbContext.SaveChangesAsync();
}

internal sealed class DemoUserInfo
{
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Password { get; init; }
    public int? RoleId { get; init; }
    public string? Address { get; init; }
}