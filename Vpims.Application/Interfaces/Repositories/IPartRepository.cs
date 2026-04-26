using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface IPartRepository
{
    Task<IReadOnlyList<Part>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Part?> GetByIdAsync(int partId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPartNumberAsync(string partNumber, CancellationToken cancellationToken = default);
    Task<Part> CreateAsync(Part part, CancellationToken cancellationToken = default);
    Task<Part> UpdateAsync(Part part, CancellationToken cancellationToken = default);
    Task DeleteAsync(Part part, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PartCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
