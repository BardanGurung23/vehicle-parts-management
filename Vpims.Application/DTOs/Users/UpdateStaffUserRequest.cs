using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Users;

public sealed class UpdateStaffUserRequest
{
    [StringLength(150, MinimumLength = 3)]
    public string? FullName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(20, MinimumLength = 7)]
    public string? PhoneNumber { get; set; }

    public bool? IsActive { get; set; }
}
