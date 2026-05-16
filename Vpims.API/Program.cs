using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Vpims.API.Middlewares;

using Vpims.Application.Common;
using Vpims.Application.Interfaces;

using Vpims.Infrastructure;
using Vpims.Infrastructure.Options;
using Vpims.Infrastructure.Persistence;
using Vpims.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

//
// Infrastructure Services
//
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.Configure<InvoiceEmailOptions>(
    builder.Configuration.GetSection(InvoiceEmailOptions.SectionName));

builder.Services.AddScoped<IStaffSalesService, StaffSalesService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddHostedService<AlertGenerationBackgroundService>();

//
// JWT Authentication
//
var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

//
// CORS
//
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:7001",
                "http://127.0.0.1:7001",
                "http://localhost:4000",
                "http://127.0.0.1:4000",
                "http://localhost:7002",
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

//
// Database Initialization / Seeding
//
using (var scope = app.Services.CreateScope())
{
    var databaseInitializer =
        scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

    await databaseInitializer.InitializeAsync(DatabaseInitializationRequest.ForStartup());
}

//
// Development Tools
//
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//
// Middleware Pipeline
//
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

//
// Routes
//
app.MapGet("/", () => Results.Redirect("/staff-sales/index.html"));

app.MapControllers();

app.Run();