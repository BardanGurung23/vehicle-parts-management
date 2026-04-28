using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class CustomerRepository(AppDbContext dbContext) : ICustomerRepository
{
    public Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return dbContext.Customers.AnyAsync(customer => customer.PhoneNumber == phoneNumber, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.Customers.AnyAsync(customer => customer.Email == email, cancellationToken);
    }

    public Task<bool> ExistsByVehicleNumberAsync(string vehicleNumber, CancellationToken cancellationToken = default)
    {
        return dbContext.Vehicles.AnyAsync(vehicle => vehicle.VehicleNumber == vehicleNumber, cancellationToken);
    }

    public async Task<Customer> RegisterCustomerAsync(
        User user,
        Customer customer,
        Vehicle? vehicle,
        CancellationToken cancellationToken = default)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        customer.UserId = user.UserId;
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (vehicle is not null)
        {
            vehicle.CustomerId = customer.CustomerId;
            dbContext.Vehicles.Add(vehicle);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return (await GetByIdAsync(customer.CustomerId, cancellationToken))!;
    }

    public async Task<Customer> CreateStaffCustomerAsync(Customer customer, Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        vehicle.CustomerId = customer.CustomerId;
        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return (await GetByIdAsync(customer.CustomerId, cancellationToken))!;
    }

    public async Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await QueryCustomers()
            .FirstOrDefaultAsync(customer => customer.CustomerId == customerId, cancellationToken);
    }

    public async Task<Customer?> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await QueryCustomers()
            .FirstOrDefaultAsync(customer => customer.CustomerId == customerId, cancellationToken);
    }

    public async Task<Customer?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await QueryCustomers()
            .FirstOrDefaultAsync(customer => customer.UserId == userId, cancellationToken);
    }

    public async Task<Customer?> GetTrackedByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Customers
            .Include(customer => customer.User)
            .Include(customer => customer.Vehicles)
            .FirstOrDefaultAsync(customer => customer.UserId == userId, cancellationToken);
    }

    public async Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(customer.CustomerId, cancellationToken))!;
    }

    public async Task<Customer> RemoveVehicleAsync(Customer customer, Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        dbContext.Vehicles.Remove(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(customer.CustomerId, cancellationToken))!;
    }
    public async Task<IReadOnlyList<Customer>> SearchAsync(
        int? customerId,
        string? phoneNumber,
        string? vehicleNumber,
        string? name,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Customer> query = QueryCustomers();

        if (customerId.HasValue)
        {
            query = query.Where(customer => customer.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            query = query.Where(customer => customer.PhoneNumber == phoneNumber);
        }

        if (!string.IsNullOrWhiteSpace(vehicleNumber))
        {
            query = query.Where(customer => customer.Vehicles.Any(vehicle => vehicle.VehicleNumber == vehicleNumber));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            string normalizedName = name.ToLowerInvariant();
            query = query.Where(customer => customer.FullName.ToLower().Contains(normalizedName));
        }

        return await query
            .OrderBy(customer => customer.FullName)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Customer> QueryCustomers()
    {
        return dbContext.Customers
            .AsNoTracking()
            .Include(customer => customer.Vehicles);
    }

    public async Task<Vehicle> AddVehicleAsync(int customerId, Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        vehicle.CustomerId = customerId;
        vehicle.CreatedAt = DateTimeOffset.UtcNow;
        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync(cancellationToken);
        return vehicle;
    }

    public async Task<IReadOnlyList<Vehicle>> GetVehiclesByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Vehicles
            .AsNoTracking()
            .Where(v => v.CustomerId == customerId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}