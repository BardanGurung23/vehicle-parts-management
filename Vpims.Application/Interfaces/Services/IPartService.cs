using Vpims.Application.DTOs.Parts;

namespace Vpims.Application.Interfaces.Services;

public interface IPartService
{
    Task<IReadOnlyList<PartResponse>> GetAllPartsAsync(CancellationToken cancellationToken = default);
    Task<PartResponse> GetPartByIdAsync(int partId, CancellationToken cancellationToken = default);
    Task<PartResponse> CreatePartAsync(CreatePartRequest request, CancellationToken cancellationToken = default);
    Task<PartResponse> UpdatePartAsync(int partId, UpdatePartRequest request, CancellationToken cancellationToken = default);
    Task DeletePartAsync(int partId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PartCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
