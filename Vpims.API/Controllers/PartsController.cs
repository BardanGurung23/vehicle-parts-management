using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Parts;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Staff")]
[Route("api/parts")]
public sealed class PartsController(IPartService partService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PartResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var parts = await partService.GetAllPartsAsync(cancellationToken);
        return Ok(parts);
    }

    [HttpGet("{partId:int}")]
    public async Task<ActionResult<PartResponse>> GetById(int partId, CancellationToken cancellationToken)
    {
        var part = await partService.GetPartByIdAsync(partId, cancellationToken);
        return Ok(part);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<PartCategoryResponse>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await partService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType<PartResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PartResponse>> Create([FromBody] CreatePartRequest request, CancellationToken cancellationToken)
    {
        var part = await partService.CreatePartAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { partId = part.PartId }, part);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{partId:int}")]
    public async Task<ActionResult<PartResponse>> Update(int partId, [FromBody] UpdatePartRequest request, CancellationToken cancellationToken)
    {
        var part = await partService.UpdatePartAsync(partId, request, cancellationToken);
        return Ok(part);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{partId:int}")]
    public async Task<IActionResult> Delete(int partId, CancellationToken cancellationToken)
    {
        await partService.DeletePartAsync(partId, cancellationToken);
        return NoContent();
    }
}
