using System.Text;

namespace Vpims.Infrastructure.Services;

internal static class InputNormalizer
{
    public static string NormalizeEmail(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    public static string NormalizeFullName(string value)
    {
        return string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static string NormalizePhoneNumber(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (char character in value.Trim())
        {
            if (char.IsDigit(character) || (character == '+' && builder.Length == 0))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}