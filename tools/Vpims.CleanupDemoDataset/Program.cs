using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vpims.Domain.Entities;
using Vpims.Infrastructure;
using Vpims.Infrastructure.Persistence;

string apiDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Vpims.API"));
string webRootPath = Path.Combine(apiDirectory, "wwwroot");

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile(Path.Combine(apiDirectory, "appsettings.json"), optional: false)
    .AddJsonFile(Path.Combine(apiDirectory, "appsettings.Development.json"), optional: true)
    .AddEnvironmentVariables();

builder.Services.AddInfrastructureServices(builder.Configuration, webRootPath);

using IHost host = builder.Build();
using IServiceScope scope = host.Services.CreateScope();

var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

string[] keepEmails =
[
    "demo.admin1@autonix.local",
    "demo.admin2@autonix.local",
    "demo.staff1@autonix.local",
    "demo.staff2@autonix.local",
    "demo.customer1@autonix.local",
    "demo.customer2@autonix.local",
    "demo.customer3@autonix.local"
];

List<(int UserId, string Email)> usersToRemove = await dbContext.Users
    .Where(user => !keepEmails.Contains(user.Email))
    .OrderBy(user => user.UserId)
    .Select(user => new ValueTuple<int, string>(user.UserId, user.Email))
    .ToListAsync();

if (usersToRemove.Count == 0)
{
    Console.WriteLine("No cleanup required. Only demo users remain.");
    return;
}

List<int> userIds = usersToRemove.Select(item => item.UserId).ToList();
List<int> customerIds = await dbContext.Customers
    .Where(customer => customer.UserId.HasValue && userIds.Contains(customer.UserId.Value))
    .Select(customer => customer.CustomerId)
    .ToListAsync();

await using var transaction = await dbContext.Database.BeginTransactionAsync();

if (userIds.Count > 0)
{
    await dbContext.Sales
        .Where(sale => sale.CreatedByUserId != 0 && userIds.Contains(sale.CreatedByUserId))
        .ExecuteDeleteAsync();

    await dbContext.PurchaseInvoices
        .Where(invoice => userIds.Contains(invoice.CreatedByUserId))
        .ExecuteDeleteAsync();
}

if (customerIds.Count > 0)
{
    await dbContext.Sales
        .Where(sale => customerIds.Contains(sale.CustomerId))
        .ExecuteDeleteAsync();
}

if (customerIds.Count > 0)
{
    await dbContext.Customers
        .Where(customer => customerIds.Contains(customer.CustomerId))
        .ExecuteDeleteAsync();
}

await dbContext.Users
    .Where(user => userIds.Contains(user.UserId))
    .ExecuteDeleteAsync();

await transaction.CommitAsync();

Console.WriteLine("Removed users:");
foreach ((int _, string email) in usersToRemove)
{
    Console.WriteLine($"- {email}");
}

Console.WriteLine();
Console.WriteLine($"Removed user count: {usersToRemove.Count}");
Console.WriteLine($"Removed customer count: {customerIds.Count}");
Console.WriteLine($"Remaining user count: {await dbContext.Users.CountAsync()}");
Console.WriteLine($"Remaining customer count: {await dbContext.Customers.CountAsync()}");