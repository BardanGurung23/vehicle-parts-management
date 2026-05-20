using System.ComponentModel.DataAnnotations;

namespace Vpims.Application.DTOs.Auth;

public sealed class ValidatePasswordResetTokenRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}