using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByVehicleNumberAsync(string vehicleNumber, CancellationToken cancellationToken = default);

    Task<Customer> RegisterCustomerAsync(User user, Customer customer, Vehicle? vehicle, CancellationToken cancellationToken = default);

    Task<Customer> CreateStaffCustomerAsync(Customer customer, Vehicle vehicle, CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdAsync(int customerId, CancellationToken cancellationToken = default);

    Task<Customer?> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);

    Task<Customer?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

<<<<<<< HEAD
    Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
    Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default);

    Task<Customer> AddVehicleAsync(Customer customer, Vehicle vehicle, CancellationToken cancellationToken = default);

>>>>>>> 2da9d9e (feat: Implement customer profile update and vehicle management features)
        string? phoneNumber,
        string? vehicleNumber,
        string? name,
        CancellationToken cancellationToken = default);

    Task<Vehicle> AddVehicleAsync(int customerId, Vehicle vehicle, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehicle>> GetVehiclesByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
}