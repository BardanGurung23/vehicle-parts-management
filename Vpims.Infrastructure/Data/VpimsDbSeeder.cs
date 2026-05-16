using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vpims.Domain.Models;

namespace Vpims.Infrastructure.Data;

public sealed class VpimsDbSeeder(
    VpimsDbContext dbContext,
    ILogger<VpimsDbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Customers.AnyAsync(cancellationToken))
        {
            dbContext.Customers.AddRange(
                new Customer
                {
                    Id = Guid.Parse("4de6dc17-22fd-4c78-9e74-cba9817f4701"),
                    FullName = "Aarav Sharma",
                    Email = "aarav.sharma@example.com"
                },
                new Customer
                {
                    Id = Guid.Parse("8d0d39b7-095a-4225-b1af-dd89d3909d65"),
                    FullName = "Sita Thapa",
                    Email = "sita.thapa@example.com"
                },
                new Customer
                {
                    Id = Guid.Parse("516d2449-67f1-4eb3-814e-5825300de131"),
                    FullName = "Rohan Gurung",
                    Email = "rohan.gurung@example.com"
                });
        }

        if (!await dbContext.VehicleParts.AnyAsync(cancellationToken))
        {
            dbContext.VehicleParts.AddRange(
                new VehiclePart
                {
                    Id = Guid.Parse("2e461849-736d-40d8-b1e0-c5441548c32a"),
                    PartNumber = "ENG-001",
                    Name = "Engine Oil Filter",
                    StockQuantity = 25,
                    UnitPrice = 650m
                },
                new VehiclePart
                {
                    Id = Guid.Parse("6fd29f84-b56c-4a76-b9db-c274729f4407"),
                    PartNumber = "BRK-014",
                    Name = "Front Brake Pad Set",
                    StockQuantity = 15,
                    UnitPrice = 2800m
                },
                new VehiclePart
                {
                    Id = Guid.Parse("db40bbf0-cd87-4f30-9cb7-4e7d36bfe3d2"),
                    PartNumber = "BAT-007",
                    Name = "12V Battery",
                    StockQuantity = 10,
                    UnitPrice = 6200m
                },
                new VehiclePart
                {
                    Id = Guid.Parse("be3ca2d7-b9ef-47b1-81a0-c0b123f347c8"),
                    PartNumber = "TYR-021",
                    Name = "All-Season Tire",
                    StockQuantity = 20,
                    UnitPrice = 4500m
                });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Vehicle Parts Management seed data is ready.");
    }
}
