using Vpims.Domain.Entities;

namespace Vpims.Application.Interfaces.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken> CreateAsync(PasswordResetToken passwordResetToken, CancellationToken cancellationToken = default);

    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task InvalidateActiveTokensForUserAsync(int userId, DateTimeOffset invalidatedAt, CancellationToken cancellationToken = default);

    Task InvalidateOtherActiveTokensForUserAsync(
        int userId,
        int passwordResetTokenId,
        DateTimeOffset invalidatedAt,
        CancellationToken cancellationToken = default);
}