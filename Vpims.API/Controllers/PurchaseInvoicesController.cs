using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.PurchaseInvoices;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/admin/purchase-invoices")]
[Authorize(Roles = "Admin")]
public sealed class PurchaseInvoicesController(
    IPurchaseInvoiceService purchaseInvoiceService,
    IAuthService authService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PurchaseInvoiceResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PurchaseInvoiceResponse>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<PurchaseInvoiceResponse> invoices = await purchaseInvoiceService.GetPurchaseInvoicesAsync(cancellationToken);
        return Ok(invoices);
    }

    [HttpGet("{purchaseInvoiceId:int}")]
    [ProducesResponseType<PurchaseInvoiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseInvoiceResponse>> GetById(int purchaseInvoiceId, CancellationToken cancellationToken)
    {
        PurchaseInvoiceResponse invoice = await purchaseInvoiceService.GetPurchaseInvoiceByIdAsync(purchaseInvoiceId, cancellationToken);
        return Ok(invoice);
    }

    [HttpPost]
    [ProducesResponseType<PurchaseInvoiceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseInvoiceResponse>> Create(
        [FromBody] CreatePurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        PurchaseInvoiceResponse invoice = await purchaseInvoiceService.CreatePurchaseInvoiceAsync(request, currentUser, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { purchaseInvoiceId = invoice.PurchaseInvoiceId }, invoice);
    }
}