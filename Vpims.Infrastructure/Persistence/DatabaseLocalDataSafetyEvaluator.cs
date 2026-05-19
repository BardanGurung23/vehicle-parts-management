using Microsoft.Extensions.Options;

namespace Vpims.Infrastructure.Persistence;

public sealed class DatabaseLocalDataSafetyEvaluator(IOptions<DatabaseInitializationOptions> options)
{
    public DatabaseLocalDataStatus Evaluate(IReadOnlyCollection<string> persistedEmails)
    {
        if (persistedEmails.Count == 0)
        {
            return DatabaseLocalDataStatus.NoUserData;
        }

        HashSet<string> protectedEmails = options.Value.ProtectedDemoUserEmails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (protectedEmails.Count == 0)
        {
            return DatabaseLocalDataStatus.Unknown;
        }

        bool onlyProtectedData = persistedEmails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .All(protectedEmails.Contains);

        return onlyProtectedData
            ? DatabaseLocalDataStatus.DemoDataOnly
            : DatabaseLocalDataStatus.ContainsNonDemoData;
    }
}