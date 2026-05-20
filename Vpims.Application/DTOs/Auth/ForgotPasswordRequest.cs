using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Auth;

public sealed class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}