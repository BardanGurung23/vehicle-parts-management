namespace Vpims.Application.DTOs.Users;

public sealed class RoleOptionResponse
{
    public int RoleId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}