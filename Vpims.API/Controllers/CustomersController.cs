using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Customers;
using Vpims.Application.DTOs.Appointments;
using Vpims.Application.DTOs.Sales;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(
    ICustomerService customerService,
    IAuthService authService,
    IAppointmentService appointmentService,
    ISaleService saleService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<RegisterCustomerResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RegisterCustomerResponse>> Register(
        [FromBody] RegisterCustomerRequest request,
        CancellationToken cancellationToken)
    {
        RegisterCustomerResponse response = await customerService.RegisterAsync(request, cancellationToken);
        return Created($"/api/customers/{response.CustomerId}", response);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    [ProducesResponseType<CustomerDetailResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomerDetailResponse>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        CustomerDetailResponse response = await customerService.CreateCustomerAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { customerId = response.CustomerId }, response);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<CustomerSearchResultResponse>>> Search(
        [FromQuery] SearchCustomersRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomerSearchResultResponse> response = await customerService.SearchCustomersAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("me")]
    public async Task<ActionResult<CustomerDetailResponse>> GetCurrentCustomer(CancellationToken cancellationToken)
    {
        UserProfileResponse currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        CustomerDetailResponse response = await customerService.GetCustomerByUserIdAsync(currentUser.UserId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Customer")]
    [HttpPut("me")]
    [ProducesResponseType<CustomerDetailResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerDetailResponse>> UpdateProfile(
        [FromBody] UpdateCustomerProfileRequest request,
        CancellationToken cancellationToken)
    {
        UserProfileResponse currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        CustomerDetailResponse response = await customerService.UpdateCustomerProfileAsync(currentUser, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("{customerId:int}")]
    public async Task<ActionResult<CustomerDetailResponse>> GetById(int customerId, CancellationToken cancellationToken)
    {
        CustomerDetailResponse response = await customerService.GetCustomerByIdAsync(customerId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("{customerId:int}/appointments")]
    [ProducesResponseType<IReadOnlyList<AppointmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> GetCustomerAppointments(
        int customerId,
        CancellationToken cancellationToken)
    {
        // Get appointments by customer ID using the repository
        var appointments = await appointmentService.GetCustomerAppointmentsByCustomerIdAsync(customerId, cancellationToken);
        return Ok(appointments);
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("{customerId:int}/sales")]
    [ProducesResponseType<IReadOnlyList<SaleResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SaleResponse>>> GetCustomerSales(
        int customerId,
        CancellationToken cancellationToken)
    {
        // Get the customer first to validate it exists
        var customer = await customerService.GetCustomerByIdAsync(customerId, cancellationToken);
        // Use the new method that accepts customerId directly
        var sales = await saleService.GetCustomerSalesByCustomerIdAsync(customerId, cancellationToken);
        return Ok(sales);
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("me/vehicles")]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<VehicleResponse>> AddVehicle(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        UserProfileResponse currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        VehicleResponse response = await customerService.AddVehicleAsync(currentUser, request, cancellationToken);
        return Created($"/api/customers/me/vehicles/{response.VehicleId}", response);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("me/vehicles")]
    [ProducesResponseType<IReadOnlyList<VehicleResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VehicleResponse>>> GetMyVehicles(CancellationToken cancellationToken)
    {
        UserProfileResponse currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        IReadOnlyList<VehicleResponse> response = await customerService.GetMyVehiclesAsync(currentUser, cancellationToken);
        return Ok(response);
    }
}