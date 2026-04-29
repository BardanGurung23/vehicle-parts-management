using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Vendors;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/admin/vendors")]
[Authorize(Roles = "Admin")]
public sealed class VendorsController : ControllerBase
{
    private readonly IVendorService _vendorService;

    public VendorsController(IVendorService vendorService)
    {
        _vendorService = vendorService;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<VendorResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VendorResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var vendors = await _vendorService.GetAllVendorsAsync(cancellationToken);
        return Ok(vendors);
    }

    [HttpGet("{vendorId:int}")]
    [ProducesResponseType<VendorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VendorResponse>> GetById(int vendorId, CancellationToken cancellationToken)
    {
        var vendor = await _vendorService.GetVendorByIdAsync(vendorId, cancellationToken);
        return Ok(vendor);
    }

    [HttpPost]
    [ProducesResponseType<VendorResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VendorResponse>> Create([FromBody] CreateVendorRequest request, CancellationToken cancellationToken)
    {
        var vendor = await _vendorService.CreateVendorAsync(request, cancellationToken);
        return Created($"/api/admin/vendors/{vendor.VendorId}", vendor);
    }

    [HttpPut("{vendorId:int}")]
    [ProducesResponseType<VendorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VendorResponse>> Update(int vendorId, [FromBody] UpdateVendorRequest request, CancellationToken cancellationToken)
    {
        var vendor = await _vendorService.UpdateVendorAsync(vendorId, request, cancellationToken);
        return Ok(vendor);
    }

    [HttpDelete("{vendorId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int vendorId, CancellationToken cancellationToken)
    {
        await _vendorService.DeleteVendorAsync(vendorId, cancellationToken);
        return NoContent();
    }
}
