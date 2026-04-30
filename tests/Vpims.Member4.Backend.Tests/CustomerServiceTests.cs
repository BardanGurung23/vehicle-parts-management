using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Auth;
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
    public async Task SearchCustomersAsync_FiltersByAllSupportedFields()
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

        IReadOnlyList<CustomerSearchResultResponse> phoneResults = await harness.Service.SearchCustomersAsync(new SearchCustomersRequest
        {
            PhoneNumber = "+9779800001111"
        });

        CustomerSearchResultResponse phoneMatch = Assert.Single(phoneResults);
        Assert.Equal(firstCustomer.CustomerId, phoneMatch.CustomerId);

        IReadOnlyList<CustomerSearchResultResponse> customerIdResults = await harness.Service.SearchCustomersAsync(new SearchCustomersRequest
        {
            CustomerId = firstCustomer.CustomerId
        });

        CustomerSearchResultResponse customerIdMatch = Assert.Single(customerIdResults);
        Assert.Equal(firstCustomer.CustomerId, customerIdMatch.CustomerId);
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
    public async Task UpdateCustomerProfileAsync_UpdatesCustomerAndLinkedUser()
    {
        await using var harness = await TestHarness.CreateAsync();

        RegisterCustomerResponse registeredCustomer = await harness.Service.RegisterAsync(new RegisterCustomerRequest
        {
            FullName = "Profile Customer",
            Email = "profile.customer@autonix.local",
            PhoneNumber = "+9779801234567",
            Password = "DemoPass123!"
        });

        UserProfileResponse currentUser = CreateCurrentUserProfile(registeredCustomer);

        CustomerDetailResponse updatedCustomer = await harness.Service.UpdateCustomerProfileAsync(currentUser, new UpdateCustomerProfileRequest
        {
            FullName = "Updated Customer",
            Email = "updated.customer@autonix.local",
            PhoneNumber = "+9779807654321",
            Address = "Bhaktapur"
        });

        Customer storedCustomer = await harness.DbContext.Customers.Include(customer => customer.User).SingleAsync();
        User storedUser = Assert.IsType<User>(storedCustomer.User);

        Assert.Equal("Updated Customer", updatedCustomer.FullName);
        Assert.Equal("updated.customer@autonix.local", updatedCustomer.Email);
        Assert.Equal("+9779807654321", updatedCustomer.PhoneNumber);
        Assert.Equal("Bhaktapur", updatedCustomer.Address);
        Assert.Equal("Updated Customer", storedCustomer.FullName);
        Assert.Equal("updated.customer@autonix.local", storedCustomer.Email);
        Assert.Equal("+9779807654321", storedCustomer.PhoneNumber);
        Assert.Equal("Updated Customer", storedUser.FullName);
        Assert.Equal("updated.customer@autonix.local", storedUser.Email);
        Assert.Equal("+9779807654321", storedUser.PhoneNumber);
    }

    [Fact]
    public async Task AddVehicleAsync_AddsVehicleForCurrentCustomer()
    {
        await using var harness = await TestHarness.CreateAsync();

        RegisterCustomerResponse registeredCustomer = await harness.Service.RegisterAsync(new RegisterCustomerRequest
        {
            FullName = "Vehicle Customer",
            Email = "vehicle.customer@autonix.local",
            PhoneNumber = "+9779808881111",
            Password = "DemoPass123!"
        });

        UserProfileResponse currentUser = CreateCurrentUserProfile(registeredCustomer);

        VehicleResponse createdVehicle = await harness.Service.AddVehicleAsync(currentUser, new CreateVehicleRequest
        {
            VehicleNumber = "ba 8 pa 8080",
            Model = "Aqua"
        });

        IReadOnlyList<VehicleResponse> vehicles = await harness.Service.GetMyVehiclesAsync(currentUser);

        Assert.Equal("BA 8 PA 8080", createdVehicle.VehicleNumber);
        Assert.Equal("Aqua", createdVehicle.Model);
        VehicleResponse storedVehicle = Assert.Single(vehicles);
        Assert.Equal(createdVehicle.VehicleId, storedVehicle.VehicleId);
    }

    [Fact]
    public async Task UpdateVehicleAsync_UpdatesExistingVehicleDetails()
    {
        await using var harness = await TestHarness.CreateAsync();

        RegisterCustomerResponse registeredCustomer = await harness.Service.RegisterAsync(new RegisterCustomerRequest
        {
            FullName = "Update Vehicle Customer",
            Email = "update.vehicle@autonix.local",
            PhoneNumber = "+9779805551111",
            Password = "DemoPass123!",
            VehicleNumber = "ba 3 pa 3003",
            VehicleModel = "Swift"
        });

        UserProfileResponse currentUser = CreateCurrentUserProfile(registeredCustomer);
        CustomerDetailResponse existingCustomer = await harness.Service.GetCustomerByUserIdAsync(registeredCustomer.UserId);

        VehicleResponse updatedVehicle = await harness.Service.UpdateVehicleAsync(currentUser, existingCustomer.Vehicles[0].VehicleId, new UpdateVehicleRequest
        {
            VehicleNumber = "ba 3 pa 3333",
            Model = "Baleno"
        });

        IReadOnlyList<VehicleResponse> vehicles = await harness.Service.GetMyVehiclesAsync(currentUser);

        Assert.Equal(existingCustomer.Vehicles[0].VehicleId, updatedVehicle.VehicleId);
        Assert.Equal("BA 3 PA 3333", updatedVehicle.VehicleNumber);
        Assert.Equal("Baleno", updatedVehicle.Model);
        Assert.Equal("BA 3 PA 3333", Assert.Single(vehicles).VehicleNumber);
    }

    [Fact]
    public async Task RemoveVehicleAsync_RemovesVehicleFromCurrentCustomer()
    {
        await using var harness = await TestHarness.CreateAsync();

        RegisterCustomerResponse registeredCustomer = await harness.Service.RegisterAsync(new RegisterCustomerRequest
        {
            FullName = "Remove Vehicle Customer",
            Email = "remove.vehicle@autonix.local",
            PhoneNumber = "+9779804441111",
            Password = "DemoPass123!",
            VehicleNumber = "ba 4 pa 4004",
            VehicleModel = "Civic"
        });

        UserProfileResponse currentUser = CreateCurrentUserProfile(registeredCustomer);
        CustomerDetailResponse existingCustomer = await harness.Service.GetCustomerByUserIdAsync(registeredCustomer.UserId);

        CustomerDetailResponse updatedCustomer = await harness.Service.RemoveVehicleAsync(currentUser, existingCustomer.Vehicles[0].VehicleId);

        Assert.Empty(updatedCustomer.Vehicles);
        Assert.Empty(harness.DbContext.Vehicles);
    }

    [Fact]
    public async Task GetMyVehiclesAsync_ReturnsAllVehiclesForCurrentCustomer()
    {
        await using var harness = await TestHarness.CreateAsync();

        RegisterCustomerResponse registeredCustomer = await harness.Service.RegisterAsync(new RegisterCustomerRequest
        {
            FullName = "Fleet Customer",
            Email = "fleet.customer@autonix.local",
            PhoneNumber = "+9779802221111",
            Password = "DemoPass123!"
        });

        UserProfileResponse currentUser = CreateCurrentUserProfile(registeredCustomer);

        await harness.Service.AddVehicleAsync(currentUser, new CreateVehicleRequest
        {
            VehicleNumber = "ba 1 pa 1001",
            Model = "Fit"
        });

        await harness.Service.AddVehicleAsync(currentUser, new CreateVehicleRequest
        {
            VehicleNumber = "ba 2 pa 2002",
            Model = "Corolla"
        });

        IReadOnlyList<VehicleResponse> vehicles = await harness.Service.GetMyVehiclesAsync(currentUser);

        Assert.Equal(2, vehicles.Count);
        Assert.Contains(vehicles, vehicle => vehicle.VehicleNumber == "BA 1 PA 1001");
        Assert.Contains(vehicles, vehicle => vehicle.VehicleNumber == "BA 2 PA 2002");
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

    private static UserProfileResponse CreateCurrentUserProfile(RegisterCustomerResponse registeredCustomer)
    {
        return new UserProfileResponse
        {
            UserId = registeredCustomer.UserId,
            CustomerId = registeredCustomer.CustomerId,
            FullName = registeredCustomer.FullName,
            Email = registeredCustomer.Email,
            PhoneNumber = registeredCustomer.PhoneNumber,
            Role = SystemRoles.Customer,
            IsActive = true
        };
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