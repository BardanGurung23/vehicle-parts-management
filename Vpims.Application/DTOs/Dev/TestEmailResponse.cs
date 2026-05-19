namespace Vpims.Application.DTOs.Dev;

public sealed class TestEmailResponse
{
    public string RecipientEmail { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}