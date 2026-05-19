using Microsoft.EntityFrameworkCore;
using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Parts;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Application.Interfaces.Services;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Services;

public sealed class PartService(IPartRepository partRepository, IPartImageStorage partImageStorage) : IPartService
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

    public async Task<PartResponse> CreatePartAsync(CreatePartRequest request, PartImageUpload? imageUpload = null, CancellationToken cancellationToken = default)
    {
        if (await partRepository.ExistsByPartNumberAsync(request.PartNumber.Trim(), cancellationToken))
            throw new AppValidationException($"A part with number '{request.PartNumber}' already exists.");

        await EnsureCategoryExistsAsync(request.PartCategoryId, cancellationToken);

        string? imageUrl = imageUpload is null
            ? NormalizeOptionalText(request.ImageUrl)
            : await partImageStorage.SaveAsync(imageUpload, cancellationToken);

        var part = new Part
        {
            PartNumber = request.PartNumber.Trim(),
            PartName = request.PartName.Trim(),
            Description = NormalizeOptionalText(request.Description),
            ImageUrl = imageUrl,
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

    public async Task<PartResponse> UpdatePartAsync(int partId, UpdatePartRequest request, PartImageUpload? imageUpload = null, CancellationToken cancellationToken = default)
    {
        var part = await partRepository.GetByIdAsync(partId, cancellationToken)
            ?? throw new NotFoundException($"Part with id {partId} not found.");

        await EnsureCategoryExistsAsync(request.PartCategoryId, cancellationToken);

        string? originalImageUrl = part.ImageUrl;
        string? requestedImageUrl = NormalizeOptionalText(request.ImageUrl);
        string? nextImageUrl = originalImageUrl;

        if (imageUpload is not null)
        {
            nextImageUrl = await partImageStorage.SaveAsync(imageUpload, cancellationToken);
        }
        else if (request.RemoveImage)
        {
            nextImageUrl = null;
        }
        else if (requestedImageUrl is not null)
        {
            nextImageUrl = requestedImageUrl;
        }

        part.PartName = request.PartName.Trim();
        part.Description = NormalizeOptionalText(request.Description);
        part.ImageUrl = nextImageUrl;
        part.UnitPrice = request.UnitPrice;
        part.CostPrice = request.CostPrice;
        part.StockQuantity = request.StockQuantity;
        part.ReorderLevel = request.ReorderLevel;
        part.PartCategoryId = request.PartCategoryId;

        if (!string.Equals(originalImageUrl, nextImageUrl, StringComparison.Ordinal))
        {
            await partImageStorage.DeleteIfManagedAsync(originalImageUrl, cancellationToken);
        }

        var updated = await partRepository.UpdateAsync(part, cancellationToken);
        return ToResponse(updated);
    }

    public async Task DeletePartAsync(int partId, CancellationToken cancellationToken = default)
    {
        var part = await partRepository.GetByIdAsync(partId, cancellationToken)
            ?? throw new NotFoundException($"Part with id {partId} not found.");

        try
        {
            await partRepository.DeleteAsync(part, cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new AppValidationException("This part cannot be deleted because it is already referenced by sales or purchase invoices.");
        }
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
        ImageUrl = part.ImageUrl,
        UnitPrice = part.UnitPrice,
        CostPrice = part.CostPrice,
        StockQuantity = part.StockQuantity,
        ReorderLevel = part.ReorderLevel,
        PartCategoryId = part.PartCategoryId,
        CategoryName = part.Category?.CategoryName,
        CreatedAt = part.CreatedAt
    };

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
