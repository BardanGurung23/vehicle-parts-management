using Microsoft.AspNetCore.Mvc;
using Vpims.Application.Common;
using Vpims.Application.DTOs.StaffSales;
using Vpims.Application.Interfaces;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/staff-sales")]
public class StaffSalesController(IStaffSalesService staffSalesService) : ControllerBase
{
    [HttpGet("customers")]
    [ProducesResponseType(typeof(IReadOnlyCollection<CustomerLookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchCustomers([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var customers = await staffSalesService.SearchCustomersAsync(search, cancellationToken);
        return Ok(customers);
    }

    [HttpGet("parts")]
    [ProducesResponseType(typeof(IReadOnlyCollection<VehiclePartLookupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchParts([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var parts = await staffSalesService.SearchPartsAsync(search, cancellationToken);
        return Ok(parts);
    }

    [HttpPost("invoices")]
    [ProducesResponseType(typeof(InvoiceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateSaleRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await staffSalesService.CreateSaleAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, invoice);
        }
        catch (AppValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (NotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPost("invoices/{saleId:guid}/send-email")]
    [ProducesResponseType(typeof(SendInvoiceEmailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendInvoiceEmail(Guid saleId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await staffSalesService.SendInvoiceEmailAsync(saleId, cancellationToken);
            return Ok(response);
        }
        catch (AppValidationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (NotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}
