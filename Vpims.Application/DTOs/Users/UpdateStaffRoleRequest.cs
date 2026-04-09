using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Users;

public sealed class UpdateStaffRoleRequest
{
    [Range(1, int.MaxValue)]
    public int RoleId { get; set; }
}