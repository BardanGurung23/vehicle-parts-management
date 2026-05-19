using Vpims.Application.Common.Exceptions;
using Vpims.Application.DTOs.Parts;
using Vpims.Application.Interfaces.Services;

namespace Vpims.Infrastructure.Services;

public sealed class LocalPartImageStorage(string partsUploadRootPath) : IPartImageStorage
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
    };

    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public async Task<string> SaveAsync(PartImageUpload imageUpload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageUpload);

        if (imageUpload.Length <= 0)
        {
            throw new AppValidationException("Part image upload is empty.");
        }

        if (imageUpload.Length > MaxFileSizeBytes)
        {
            throw new AppValidationException("Part image must be 5 MB or smaller.");
        }

        string extension = Path.GetExtension(imageUpload.FileName)?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!AllowedContentTypes.TryGetValue(extension, out string? expectedContentType))
        {
            throw new AppValidationException("Only JPG, PNG, and WebP part images are supported.");
        }

        string? submittedContentType = imageUpload.ContentType?.Trim();
        if (!string.IsNullOrWhiteSpace(submittedContentType) && !string.Equals(submittedContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new AppValidationException("Uploaded part image type does not match the file extension.");
        }

        Directory.CreateDirectory(partsUploadRootPath);

        string fileName = $"{Guid.NewGuid():N}{extension}";
        string filePath = Path.Combine(partsUploadRootPath, fileName);

        await using var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await imageUpload.Content.CopyToAsync(fileStream, cancellationToken);

        return $"/uploads/parts/{fileName}";
    }

    public async Task DeleteIfManagedAsync(string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (!IsManagedUrl(imageUrl))
        {
            return;
        }

        string managedPath = imageUrl!.Trim().TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(partsUploadRootPath)!, managedPath["uploads".Length..].TrimStart(Path.DirectorySeparatorChar)));

        string uploadsRoot = Path.GetFullPath(partsUploadRootPath);
        if (!fullPath.StartsWith(uploadsRoot, PathComparison))
        {
            return;
        }

        if (!File.Exists(fullPath))
        {
            return;
        }

        await Task.Run(() => File.Delete(fullPath), cancellationToken);
    }

    public bool IsManagedUrl(string? imageUrl)
    {
        return !string.IsNullOrWhiteSpace(imageUrl)
            && imageUrl.Trim().StartsWith("/uploads/parts/", StringComparison.OrdinalIgnoreCase);
    }
}