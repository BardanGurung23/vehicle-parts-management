using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Sales;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize(Roles = "Customer")]
public sealed class SalesController : ControllerBase
{
    private readonly ISaleService _salesService;

    public SalesController(ISaleService salesService)
    {
        _salesService = salesService;
    }

    [HttpGet("me")]
    [ProducesResponseType<IReadOnlyList<SaleResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SaleResponse>>> GetMySales(CancellationToken cancellationToken)
    {
        var user = GetCurrentUserProfile();
        var sales = await _salesService.GetCustomerSalesAsync(user, cancellationToken);
        return Ok(sales);
    }

    [HttpGet("{saleId:int}")]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaleResponse>> GetById(int saleId, CancellationToken cancellationToken)
    {
        var user = GetCurrentUserProfile();
        var sale = await _salesService.GetSaleByIdAsync(saleId, user, cancellationToken);
        return Ok(sale);
    }

    [HttpPost]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SaleResponse>> CreateSale(
        CreateSaleRequest request,
        CancellationToken cancellationToken)
    {
        var user = GetCurrentUserProfile();
        var sale = await _salesService.CreateSaleAsync(request, user, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { saleId = sale.SaleId }, sale);
    }

    private UserProfileResponse GetCurrentUserProfile()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID not found in token.");

        return new UserProfileResponse
        {
            UserId = int.Parse(userIdStr),
            Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "",
            FullName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "",
            Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "",
        };
    }
}
