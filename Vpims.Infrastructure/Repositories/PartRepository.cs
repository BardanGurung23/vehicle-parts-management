using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class PartRepository(AppDbContext dbContext) : IPartRepository
{
    public async Task<IReadOnlyList<Part>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Parts
            .AsNoTracking()
            .Include(p => p.Category)
            .OrderBy(p => p.PartName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Part?> GetByIdAsync(int partId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Parts
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.PartId == partId, cancellationToken);
    }

    public Task<bool> ExistsByPartNumberAsync(string partNumber, CancellationToken cancellationToken = default)
    {
        return dbContext.Parts.AnyAsync(p => p.PartNumber == partNumber, cancellationToken);
    }

    public Task<bool> CategoryExistsAsync(int partCategoryId, CancellationToken cancellationToken = default)
    {
        return dbContext.PartCategories.AnyAsync(category => category.PartCategoryId == partCategoryId, cancellationToken);
    }

    public async Task<Part> CreateAsync(Part part, CancellationToken cancellationToken = default)
    {
        dbContext.Parts.Add(part);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(part.PartId, cancellationToken))!;
    }

    public async Task<Part> UpdateAsync(Part part, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(part.PartId, cancellationToken))!;
    }

    public async Task DeleteAsync(Part part, CancellationToken cancellationToken = default)
    {
        dbContext.Parts.Remove(part);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PartCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.PartCategories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .ToListAsync(cancellationToken);
    }
}
