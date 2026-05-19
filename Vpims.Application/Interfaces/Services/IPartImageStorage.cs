using Vpims.Application.DTOs.Parts;

namespace Vpims.Application.Interfaces.Services;

public interface IPartImageStorage
{
    Task<string> SaveAsync(PartImageUpload imageUpload, CancellationToken cancellationToken = default);
    Task DeleteIfManagedAsync(string? imageUrl, CancellationToken cancellationToken = default);
    bool IsManagedUrl(string? imageUrl);
}