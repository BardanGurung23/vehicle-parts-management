namespace Vpims.Application.DTOs.Parts;

public sealed record PartImageUpload(
    string FileName,
    string? ContentType,
    Stream Content,
    long Length);