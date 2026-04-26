using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Customers;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(ICustomerService customerService, IAuthService authService) : ControllerBase
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

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("{customerId:int}")]
    public async Task<ActionResult<CustomerDetailResponse>> GetById(int customerId, CancellationToken cancellationToken)
    {
        CustomerDetailResponse response = await customerService.GetCustomerByIdAsync(customerId, cancellationToken);
        return Ok(response);
    }
}