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

var roles = await dbContext.Roles
    .AsNoTracking()
    .ToListAsync();

var roleMap = roles.ToDictionary(role => role.Name, role => role.RoleId, StringComparer.OrdinalIgnoreCase);

await EnsurePartCategoriesAsync(dbContext);

var demoAccounts = new DemoAccount[]
{
    DemoAccount.Staff("Demo Admin One", "demo.admin1@autonix.local", "+9779801001001", "Admin", "DemoPass123!"),
    DemoAccount.Staff("Demo Admin Two", "demo.admin2@autonix.local", "+9779801001002", "Admin", "DemoPass123!"),
    DemoAccount.Staff("Demo Staff One", "demo.staff1@autonix.local", "+9779802002001", "Staff", "DemoPass123!"),
    DemoAccount.Staff("Demo Staff Two", "demo.staff2@autonix.local", "+9779802002002", "Staff", "DemoPass123!"),
    DemoAccount.Customer("Demo Customer One", "demo.customer1@autonix.local", "+9779803003001", "Kathmandu, Nepal", "DemoPass123!"),
    DemoAccount.Customer("Demo Customer Two", "demo.customer2@autonix.local", "+9779803003002", "Pokhara, Nepal", "DemoPass123!"),
    DemoAccount.Customer("Demo Customer Three", "demo.customer3@autonix.local", "+9779803003003", "Lalitpur, Nepal", "DemoPass123!")
};

foreach (DemoAccount account in demoAccounts)
{
    User? existingUser = await dbContext.Users
        .Include(user => user.Customer)
        .FirstOrDefaultAsync(user => user.Email == account.Email);

    if (existingUser is not null)
    {
        await RepairExistingDemoAccountAsync(dbContext, passwordHasher, roleMap, existingUser, account);
        Console.WriteLine($"Repaired existing account: {account.Email}");
        continue;
    }

    if (account.Kind == DemoAccountKind.Customer)
    {
        var customerResult = await customerService.RegisterAsync(new RegisterCustomerRequest
        {
            FullName = account.FullName,
            Email = account.Email,
            PhoneNumber = account.PhoneNumber,
            Address = account.Address,
            Password = account.Password
        });

        Console.WriteLine($"Created customer: {customerResult.Email} ({customerResult.CustomerId})");
        continue;
    }

    if (!roleMap.TryGetValue(account.RoleName!, out int roleId))
    {
        throw new InvalidOperationException($"Role '{account.RoleName}' is missing in the database.");
    }

    var staffResult = await staffManagementService.RegisterStaffAsync(new CreateStaffUserRequest
    {
        FullName = account.FullName,
        Email = account.Email,
        PhoneNumber = account.PhoneNumber,
        Password = account.Password,
        RoleId = roleId
    });

    Console.WriteLine($"Created {staffResult.Role.ToLowerInvariant()}: {staffResult.Email} ({staffResult.UserId})");
}

Console.WriteLine();
Console.WriteLine("Demo login password for all seeded users: DemoPass123!");

static async Task EnsurePartCategoriesAsync(AppDbContext dbContext)
{
    PartCategory[] defaultCategories =
    [
        new() { CategoryName = "Engine", Description = "Filters, belts, sensors, and engine service parts." },
        new() { CategoryName = "Brakes", Description = "Pads, discs, cylinders, and brake hardware." },
        new() { CategoryName = "Suspension", Description = "Shocks, bushings, arms, and alignment parts." },
        new() { CategoryName = "Electrical", Description = "Batteries, lights, relays, and charging parts." }
    ];

    foreach (PartCategory category in defaultCategories)
    {
        bool exists = await dbContext.PartCategories
            .AnyAsync(existing => existing.CategoryName == category.CategoryName);

        if (!exists)
        {
            dbContext.PartCategories.Add(category);
        }
    }

    await dbContext.SaveChangesAsync();
}

static async Task RepairExistingDemoAccountAsync(
    AppDbContext dbContext,
    PasswordHasher<User> passwordHasher,
    IReadOnlyDictionary<string, int> roleMap,
    User existingUser,
    DemoAccount account)
{
    existingUser.FullName = account.FullName;
    existingUser.PhoneNumber = account.PhoneNumber;
    existingUser.IsActive = true;
    existingUser.PasswordHash = passwordHasher.HashPassword(existingUser, account.Password);

    if (account.Kind == DemoAccountKind.Customer)
    {
        if (!roleMap.TryGetValue(SystemRoles.Customer, out int customerRoleId))
        {
            throw new InvalidOperationException("Role 'Customer' is missing in the database.");
        }

        existingUser.RoleId = customerRoleId;

        if (existingUser.Customer is not null)
        {
            existingUser.Customer.FullName = account.FullName;
            existingUser.Customer.PhoneNumber = account.PhoneNumber;
            existingUser.Customer.Email = account.Email;
            existingUser.Customer.Address = account.Address;
        }
    }
    else
    {
        if (!roleMap.TryGetValue(account.RoleName!, out int roleId))
        {
            throw new InvalidOperationException($"Role '{account.RoleName}' is missing in the database.");
        }

        existingUser.RoleId = roleId;
    }

    await dbContext.SaveChangesAsync();
}

internal enum DemoAccountKind
{
    Staff,
    Customer
}

internal sealed record DemoAccount(
    DemoAccountKind Kind,
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    string? RoleName = null,
    string? Address = null)
{
    public static DemoAccount Staff(string fullName, string email, string phoneNumber, string roleName, string password)
        => new(DemoAccountKind.Staff, fullName, email, phoneNumber, password, roleName);

    public static DemoAccount Customer(string fullName, string email, string phoneNumber, string address, string password)
        => new(DemoAccountKind.Customer, fullName, email, phoneNumber, password, null, address);
}