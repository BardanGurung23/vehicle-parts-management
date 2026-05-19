using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vpims.Infrastructure;
using Vpims.Infrastructure.Persistence;

PreflightArguments arguments = PreflightArguments.Parse(args);

string apiDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Vpims.API"));
string webRootPath = Path.Combine(apiDirectory, "wwwroot");

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile(Path.Combine(apiDirectory, "appsettings.json"), optional: false)
    .AddJsonFile(Path.Combine(apiDirectory, "appsettings.Development.json"), optional: true)
    .AddEnvironmentVariables();

if (!string.IsNullOrWhiteSpace(arguments.ConnectionString))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:defaultConnection"] = arguments.ConnectionString
    });
}

builder.Services.AddInfrastructureServices(builder.Configuration, webRootPath);

using IHost host = builder.Build();
using IServiceScope scope = host.Services.CreateScope();

var databaseInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

DatabasePreflightReport initialReport = await databaseInitializer.CreatePreflightReportAsync();
PrintReport(initialReport, "Database preflight report");

if (!arguments.ApplyChanges)
{
    return initialReport.CanProceedWithoutReset ? 0 : 1;
}

if (initialReport.HostStatus is DatabaseHostStatus.Unreachable or DatabaseHostStatus.AuthenticationFailed or DatabaseHostStatus.Misconfigured)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Preflight could not continue because the configured database host is not ready.");
    return 1;
}

bool allowDestructiveReset = false;
bool allowResetWithProtectedData = false;

if (initialReport.RequiresDestructiveReset)
{
    if (initialReport.LocalDataStatus is DatabaseLocalDataStatus.ContainsNonDemoData or DatabaseLocalDataStatus.Unknown)
    {
        if (!arguments.ForceReset)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Preflight refused to reset the database because non-demo or unclassified local data was detected.");
            Console.Error.WriteLine("Back up your local data first, then rerun with --force-reset if you still want to recreate the database.");
            return 1;
        }

        allowResetWithProtectedData = true;
    }

    allowDestructiveReset = true;

    bool hasInteractiveTerminal = !arguments.NonInteractive && !Console.IsInputRedirected;
    if (!arguments.AssumeYes)
    {
        if (!hasInteractiveTerminal)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("A destructive reset is required, but the current session is non-interactive.");
            Console.Error.WriteLine("Rerun in an interactive terminal, or pass --yes after reviewing the preflight report.");
            return 1;
        }

        if (!PromptForReset(initialReport))
        {
            Console.WriteLine("Database reset cancelled.");
            return 1;
        }
    }
}

await databaseInitializer.InitializeAsync(DatabaseInitializationRequest.ForPreflight(
    allowDestructiveReset: allowDestructiveReset,
    allowResetWithProtectedData: allowResetWithProtectedData));

DatabasePreflightReport finalReport = await databaseInitializer.CreatePreflightReportAsync();
PrintReport(finalReport, "Database validation after initialization");

if (!finalReport.CanProceedWithoutReset)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Database initialization completed with validation errors.");
    return 1;
}

Console.WriteLine();
Console.WriteLine("Database preflight and initialization completed successfully.");
return 0;

static bool PromptForReset(DatabasePreflightReport report)
{
    Console.WriteLine();
    Console.WriteLine($"A destructive reset is required for database '{report.TargetDatabaseName ?? "(unknown)"}'.");
    Console.WriteLine("This will drop and recreate the configured local database before applying migrations and seed data.");

    if (report.LocalDataStatus == DatabaseLocalDataStatus.DemoDataOnly)
    {
        Console.WriteLine("Existing data matches the configured demo dataset and is considered disposable.");
    }
    else if (report.LocalDataStatus == DatabaseLocalDataStatus.ContainsNonDemoData)
    {
        Console.WriteLine("Existing data includes non-demo users. Proceed only if you have already backed it up.");
    }
    else if (report.LocalDataStatus == DatabaseLocalDataStatus.Unknown)
    {
        Console.WriteLine("Existing data could not be classified safely. Proceed only if you understand the risk.");
    }

    Console.Write("Type 'reset' to continue: ");
    string? confirmation = Console.ReadLine();
    return string.Equals(confirmation?.Trim(), "reset", StringComparison.OrdinalIgnoreCase);
}

static void PrintReport(DatabasePreflightReport report, string heading)
{
    Console.WriteLine();
    Console.WriteLine(heading);
    Console.WriteLine(new string('-', heading.Length));
    Console.WriteLine($"Provider: {report.Provider}");
    Console.WriteLine($"Target database: {report.TargetDatabaseName ?? "(not applicable)"}");
    Console.WriteLine($"Host status: {report.HostStatus}");
    Console.WriteLine($"Database exists: {report.DatabaseExists}");
    Console.WriteLine($"Schema status: {report.SchemaStatus}");

    if (report.LocalDataStatus != DatabaseLocalDataStatus.NotChecked)
    {
        Console.WriteLine($"Local data status: {report.LocalDataStatus}");
    }

    if (report.Notes.Count > 0)
    {
        Console.WriteLine("Notes:");
        foreach (string note in report.Notes)
        {
            Console.WriteLine($"- {note}");
        }
    }

    if (report.Issues.Count > 0)
    {
        Console.WriteLine("Issues:");
        foreach (string issue in report.Issues)
        {
            Console.WriteLine($"- {issue}");
        }
    }
}

internal sealed record PreflightArguments(
    bool ApplyChanges,
    bool ForceReset,
    bool AssumeYes,
    bool NonInteractive,
    string? ConnectionString)
{
    public static PreflightArguments Parse(string[] args)
    {
        bool applyChanges = false;
        bool forceReset = false;
        bool assumeYes = false;
        bool nonInteractive = false;
        string? connectionString = null;

        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];
            switch (argument)
            {
                case "--apply":
                    applyChanges = true;
                    break;
                case "--force-reset":
                    forceReset = true;
                    break;
                case "--yes":
                    assumeYes = true;
                    break;
                case "--non-interactive":
                    nonInteractive = true;
                    break;
                case "--connection-string" when index + 1 < args.Length:
                    connectionString = args[++index];
                    break;
            }
        }

        return new PreflightArguments(applyChanges, forceReset, assumeYes, nonInteractive, connectionString);
    }
}