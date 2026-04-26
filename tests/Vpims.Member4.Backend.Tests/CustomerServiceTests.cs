using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Customers;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;
using Vpims.Infrastructure.Repositories;
using Vpims.Infrastructure.Services;
using Xunit;

namespace Vpims.Member4.Backend.Tests;

public sealed class CustomerServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesLinkedCustomerAndVehicle()
    {
        await using var harness = await TestHarness.CreateAsync();

        RegisterCustomerResponse response = await harness.Service.RegisterAsync(new RegisterCustomerRequest
        {
            FullName = "  Demo   Customer  ",
            Email = "Demo.Customer@Autonix.Local",
            PhoneNumber = " +977-9801112222 ",
            Password = "DemoPass123!",
            Address = " Kathmandu ",
            VehicleNumber = "ba 1 pa 1234",
            VehicleModel = "Civic"
        });

        Customer storedCustomer = await harness.DbContext.Customers
            .Include(customer => customer.Vehicles)
            .SingleAsync();

        User storedUser = await harness.DbContext.Users.SingleAsync();
        Vehicle storedVehicle = Assert.Single(storedCustomer.Vehicles);

        Assert.Equal(storedUser.UserId, response.UserId);
        Assert.Equal(storedCustomer.CustomerId, response.CustomerId);
        Assert.Equal("Demo Customer", storedCustomer.FullName);
        Assert.Equal("+9779801112222", storedCustomer.PhoneNumber);
        Assert.Equal("demo.customer@autonix.local", storedCustomer.Email);
        Assert.Equal(storedUser.UserId, storedCustomer.UserId);
        Assert.Equal("BA 1 PA 1234", storedVehicle.VehicleNumber);
        Assert.Equal("Civic", storedVehicle.Model);
    }

    [Fact]
    public async Task CreateCustomerAsync_CreatesStaffCustomerWithoutPortalUser()
    {
        await using var harness = await TestHarness.CreateAsync();

        CustomerDetailResponse response = await harness.Service.CreateCustomerAsync(new CreateCustomerRequest
        {
            FullName = "Staff Created Customer",
            PhoneNumber = "+9779803334444",
            Email = "walkin@autonix.local",
            Address = "Pokhara",
            VehicleNumber = "ga 2 cha 4321",
            VehicleModel = "Yaris"
        });

        Customer storedCustomer = await harness.DbContext.Customers
            .Include(customer => customer.Vehicles)
            .SingleAsync();

        Assert.Null(storedCustomer.UserId);
        Assert.Equal(response.CustomerId, storedCustomer.CustomerId);
        Assert.Equal("Staff Created Customer", storedCustomer.FullName);
        Assert.Equal("+9779803334444", storedCustomer.PhoneNumber);
        Assert.Empty(harness.DbContext.Users);
        Assert.Equal("GA 2 CHA 4321", Assert.Single(storedCustomer.Vehicles).VehicleNumber);
    }

    [Fact]
    public async Task SearchCustomersAsync_FiltersByVehicleAndName()
    {
        await using var harness = await TestHarness.CreateAsync();

        CustomerDetailResponse firstCustomer = await harness.Service.CreateCustomerAsync(new CreateCustomerRequest
        {
            FullName = "Aarav Shrestha",
            PhoneNumber = "+9779800001111",
            VehicleNumber = "ba 9 pa 9999",
            VehicleModel = "Corolla"
        });

        await harness.Service.CreateCustomerAsync(new CreateCustomerRequest
        {
            FullName = "Nina Rai",
            PhoneNumber = "+9779800002222",
            VehicleNumber = "ba 5 kha 5555",
            VehicleModel = "Swift"
        });

        IReadOnlyList<CustomerSearchResultResponse> vehicleResults = await harness.Service.SearchCustomersAsync(new SearchCustomersRequest
        {
            VehicleNumber = "BA 9 PA 9999"
        });

        CustomerSearchResultResponse vehicleMatch = Assert.Single(vehicleResults);
        Assert.Equal(firstCustomer.CustomerId, vehicleMatch.CustomerId);

        IReadOnlyList<CustomerSearchResultResponse> nameResults = await harness.Service.SearchCustomersAsync(new SearchCustomersRequest
        {
            Name = "aarav"
        });

        CustomerSearchResultResponse nameMatch = Assert.Single(nameResults);
        Assert.Equal(firstCustomer.CustomerId, nameMatch.CustomerId);
        Assert.Equal(1, nameMatch.VehicleCount);
    }

    [Fact]
    public async Task GetCustomerByUserIdAsync_ReturnsCurrentCustomerDetail()
    {
        await using var harness = await TestHarness.CreateAsync();

        RegisterCustomerResponse registeredCustomer = await harness.Service.RegisterAsync(new RegisterCustomerRequest
        {
            FullName = "Portal Customer",
            Email = "portal.customer@autonix.local",
            PhoneNumber = "+9779807778888",
            Password = "DemoPass123!",
            VehicleNumber = "ba 7 pa 7007",
            VehicleModel = "City"
        });

        CustomerDetailResponse detail = await harness.Service.GetCustomerByUserIdAsync(registeredCustomer.UserId);

        Assert.Equal(registeredCustomer.CustomerId, detail.CustomerId);
        Assert.Equal("Portal Customer", detail.FullName);
        Assert.Single(detail.Vehicles);
        Assert.Equal("BA 7 PA 7007", detail.Vehicles[0].VehicleNumber);
    }

    [Fact]
    public async Task CreateCustomerAsync_RejectsDuplicateVehicleNumber()
    {
        await using var harness = await TestHarness.CreateAsync();

        await harness.Service.CreateCustomerAsync(new CreateCustomerRequest
        {
            FullName = "First Customer",
            PhoneNumber = "+9779804445555",
            VehicleNumber = "ba 4 pa 4444"
        });

        Task duplicateRequest() => harness.Service.CreateCustomerAsync(new CreateCustomerRequest
        {
            FullName = "Second Customer",
            PhoneNumber = "+9779806667777",
            VehicleNumber = "BA 4 PA 4444"
        });

        AppValidationException exception = await Assert.ThrowsAsync<AppValidationException>(duplicateRequest);
        Assert.Equal("A vehicle with this number already exists.", exception.Message);
    }

    private sealed class TestHarness(AppDbContext dbContext, CustomerService service) : IAsyncDisposable
    {
        public AppDbContext DbContext { get; } = dbContext;

        public CustomerService Service { get; } = service;

        public static async Task<TestHarness> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var dbContext = new AppDbContext(options);

            dbContext.Roles.AddRange(
                new Role { RoleId = 1, Name = SystemRoles.Admin, Description = "Admin" },
                new Role { RoleId = 2, Name = SystemRoles.Staff, Description = "Staff" },
                new Role { RoleId = 3, Name = SystemRoles.Customer, Description = "Customer" });

            await dbContext.SaveChangesAsync();

            var service = new CustomerService(
                new CustomerRepository(dbContext),
                new UserRepository(dbContext),
                new RoleRepository(dbContext),
                new PasswordHasher<User>());

            return new TestHarness(dbContext, service);
        }

        public ValueTask DisposeAsync()
        {
            return DbContext.DisposeAsync();
        }
    }
}