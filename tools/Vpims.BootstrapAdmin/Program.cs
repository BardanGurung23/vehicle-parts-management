using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Users;
using Vpims.Application.Interfaces.Services;
using Vpims.Infrastructure;

var arguments = ParseArguments(args);

if (!arguments.TryGetValue("full-name", out string? fullName) ||
    !arguments.TryGetValue("email", out string? email) ||
    !arguments.TryGetValue("phone", out string? phoneNumber) ||
    !arguments.TryGetValue("password", out string? password))
{
    PrintUsage();
    return 1;
}

string requiredFullName = fullName ?? throw new InvalidOperationException("Full name is required.");
string requiredEmail = email ?? throw new InvalidOperationException("Email is required.");
string requiredPhoneNumber = phoneNumber ?? throw new InvalidOperationException("Phone number is required.");
string requiredPassword = password ?? throw new InvalidOperationException("Password is required.");

string apiDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Vpims.API"));

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile(Path.Combine(apiDirectory, "appsettings.json"), optional: false)
    .AddJsonFile(Path.Combine(apiDirectory, "appsettings.Development.json"), optional: true)
    .AddEnvironmentVariables();

if (arguments.TryGetValue("connection-string", out string? connectionString) && !string.IsNullOrWhiteSpace(connectionString))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:defaultConnection"] = connectionString
    });
}

builder.Services.AddInfrastructureServices(builder.Configuration);

using IHost host = builder.Build();
using IServiceScope scope = host.Services.CreateScope();

var staffManagementService = scope.ServiceProvider.GetRequiredService<IStaffManagementService>();
var roles = await staffManagementService.GetAssignableRolesAsync();
var adminRole = roles.FirstOrDefault(role => role.Name == "Admin");

if (adminRole is null)
{
    Console.Error.WriteLine("Admin role was not found. Apply the SQL schema first so the seeded roles exist.");
    return 1;
}

try
{
    StaffUserResponse response = await staffManagementService.RegisterStaffAsync(new CreateStaffUserRequest
    {
        FullName = requiredFullName,
        Email = requiredEmail,
        PhoneNumber = requiredPhoneNumber,
        Password = requiredPassword,
        RoleId = adminRole.RoleId
    });

    Console.WriteLine($"Created admin user {response.Email} with id {response.UserId}.");
    return 0;
}
catch (AppValidationException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

static Dictionary<string, string?> ParseArguments(string[] args)
{
    var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    for (int index = 0; index < args.Length; index += 2)
    {
        if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        values[args[index][2..]] = args[index + 1];
    }

    return values;
}

static void PrintUsage()
{
    Console.Error.WriteLine("Usage: dotnet run --project Vpims.BootstrapAdmin -- --full-name \"Full Name\" --email email@example.com --phone 9800000000 --password password [--connection-string \"...\"]");
}