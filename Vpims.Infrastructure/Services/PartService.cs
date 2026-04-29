using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Parts;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class PartService(IPartRepository partRepository) : IPartService
{
    public async Task<IReadOnlyList<PartResponse>> GetAllPartsAsync(CancellationToken cancellationToken = default)
    {
        var parts = await partRepository.GetAllAsync(cancellationToken);
        return parts.Select(ToResponse).ToList();
    }

    public async Task<PartResponse> GetPartByIdAsync(int partId, CancellationToken cancellationToken = default)
    {
        var part = await partRepository.GetByIdAsync(partId, cancellationToken)
            ?? throw new NotFoundException($"Part with id {partId} not found.");
        return ToResponse(part);
    }

    public async Task<PartResponse> CreatePartAsync(CreatePartRequest request, CancellationToken cancellationToken = default)
    {
        if (await partRepository.ExistsByPartNumberAsync(request.PartNumber.Trim(), cancellationToken))
            throw new AppValidationException($"A part with number '{request.PartNumber}' already exists.");

        await EnsureCategoryExistsAsync(request.PartCategoryId, cancellationToken);

        var part = new Part
        {
            PartNumber = request.PartNumber.Trim(),
            PartName = request.PartName.Trim(),
            Description = request.Description?.Trim(),
            UnitPrice = request.UnitPrice,
            CostPrice = request.CostPrice,
            StockQuantity = request.StockQuantity,
            ReorderLevel = request.ReorderLevel,
            PartCategoryId = request.PartCategoryId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await partRepository.CreateAsync(part, cancellationToken);
        return ToResponse(created);
    }

    public async Task<PartResponse> UpdatePartAsync(int partId, UpdatePartRequest request, CancellationToken cancellationToken = default)
    {
        var part = await partRepository.GetByIdAsync(partId, cancellationToken)
            ?? throw new NotFoundException($"Part with id {partId} not found.");

        await EnsureCategoryExistsAsync(request.PartCategoryId, cancellationToken);

        part.PartName = request.PartName.Trim();
        part.Description = request.Description?.Trim();
        part.UnitPrice = request.UnitPrice;
        part.CostPrice = request.CostPrice;
        part.StockQuantity = request.StockQuantity;
        part.ReorderLevel = request.ReorderLevel;
        part.PartCategoryId = request.PartCategoryId;

        var updated = await partRepository.UpdateAsync(part, cancellationToken);
        return ToResponse(updated);
    }

    public async Task DeletePartAsync(int partId, CancellationToken cancellationToken = default)
    {
        var part = await partRepository.GetByIdAsync(partId, cancellationToken)
            ?? throw new NotFoundException($"Part with id {partId} not found.");
        await partRepository.DeleteAsync(part, cancellationToken);
    }

    public async Task<IReadOnlyList<PartCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await partRepository.GetCategoriesAsync(cancellationToken);
        return categories.Select(c => new PartCategoryResponse
        {
            PartCategoryId = c.PartCategoryId,
            CategoryName = c.CategoryName,
            Description = c.Description
        }).ToList();
    }

    private async Task EnsureCategoryExistsAsync(int? partCategoryId, CancellationToken cancellationToken)
    {
        if (!partCategoryId.HasValue)
        {
            return;
        }

        if (!await partRepository.CategoryExistsAsync(partCategoryId.Value, cancellationToken))
        {
            throw new AppValidationException("Selected part category was not found.");
        }
    }

    private static PartResponse ToResponse(Part part) => new()
    {
        PartId = part.PartId,
        PartNumber = part.PartNumber ?? string.Empty,
        PartName = part.PartName ?? string.Empty,
        Description = part.Description,
        UnitPrice = part.UnitPrice,
        CostPrice = part.CostPrice,
        StockQuantity = part.StockQuantity,
        ReorderLevel = part.ReorderLevel,
        PartCategoryId = part.PartCategoryId,
        CategoryName = part.Category?.CategoryName,
        CreatedAt = part.CreatedAt
    };
}
