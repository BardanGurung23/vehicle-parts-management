using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Customers;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(ICustomerService customerService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<RegisterCustomerResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RegisterCustomerResponse>> Register(
        [FromBody] RegisterCustomerRequest request,
        CancellationToken cancellationToken)
    {
        RegisterCustomerResponse response = await customerService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Register), new { id = response.CustomerId }, response);
    }
}