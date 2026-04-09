using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<Customer> RegisterCustomerAsync(User user, Customer customer, CancellationToken cancellationToken = default);
}