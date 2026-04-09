using Microsoft.EntityFrameworkCore.Storage;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class CustomerRepository(AppDbContext dbContext) : ICustomerRepository
{
    public async Task<Customer> RegisterCustomerAsync(User user, Customer customer, CancellationToken cancellationToken = default)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        customer.UserId = user.UserId;
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return customer;
    }
}