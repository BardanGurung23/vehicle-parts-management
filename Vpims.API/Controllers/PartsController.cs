using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.API.Models.Parts;
using Vpims.Application.DTOs.Parts;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/parts")]
public sealed class PartsController(IPartService partService) : ControllerBase
{
    [Authorize(Roles = "Customer,Admin,Staff")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PartResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var parts = await partService.GetAllPartsAsync(cancellationToken);
        return Ok(parts);
    }

    [Authorize(Roles = "Customer,Admin,Staff")]
    [HttpGet("{partId:int}")]
    public async Task<ActionResult<PartResponse>> GetById(int partId, CancellationToken cancellationToken)
    {
        var part = await partService.GetPartByIdAsync(partId, cancellationToken);
        return Ok(part);
    }

    [Authorize(Roles = "Customer,Admin,Staff")]
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<PartCategoryResponse>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await partService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<PartResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PartResponse>> Create([FromForm] CreatePartFormRequest request, CancellationToken cancellationToken)
    {
        await using Stream? imageStream = request.ImageFile?.OpenReadStream();
        PartImageUpload? imageUpload = imageStream is null
            ? null
            : new PartImageUpload(request.ImageFile!.FileName, request.ImageFile.ContentType, imageStream, request.ImageFile.Length);

        var part = await partService.CreatePartAsync(new CreatePartRequest
        {
            PartNumber = request.PartNumber,
            PartName = request.PartName,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            UnitPrice = request.UnitPrice,
            CostPrice = request.CostPrice,
            StockQuantity = request.StockQuantity,
            ReorderLevel = request.ReorderLevel,
            PartCategoryId = request.PartCategoryId,
        }, imageUpload, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { partId = part.PartId }, part);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{partId:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<PartResponse>> Update(int partId, [FromForm] UpdatePartFormRequest request, CancellationToken cancellationToken)
    {
        await using Stream? imageStream = request.ImageFile?.OpenReadStream();
        PartImageUpload? imageUpload = imageStream is null
            ? null
            : new PartImageUpload(request.ImageFile!.FileName, request.ImageFile.ContentType, imageStream, request.ImageFile.Length);

        var part = await partService.UpdatePartAsync(partId, new UpdatePartRequest
        {
            PartName = request.PartName,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            RemoveImage = request.RemoveImage,
            UnitPrice = request.UnitPrice,
            CostPrice = request.CostPrice,
            StockQuantity = request.StockQuantity,
            ReorderLevel = request.ReorderLevel,
            PartCategoryId = request.PartCategoryId,
        }, imageUpload, cancellationToken);

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
