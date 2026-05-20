namespace Vpims.Infrastructure.Options;

public class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public string FrontendBaseUrl { get; set; } = string.Empty;

    public int TokenLifetimeMinutes { get; set; } = 60;
}