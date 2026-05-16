using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vpims.Infrastructure;
using Vpims.Infrastructure.Data;
using Vpims.Infrastructure.Services;

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

var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
var staffSalesSeeder = scope.ServiceProvider.GetRequiredService<VpimsDbSeeder>();
await seeder.SeedAsync();
await staffSalesSeeder.SeedAsync();

Console.WriteLine("SeedDatabase now uses the canonical Autonix demo seed path.");
Console.WriteLine("Demo login password for all seeded users: DemoPass123!");