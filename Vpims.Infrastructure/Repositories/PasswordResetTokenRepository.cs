using Microsoft.EntityFrameworkCore;
using Vpims.Application.Interfaces.Repositories;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository(AppDbContext dbContext) : IPasswordResetTokenRepository
{
    public async Task<PasswordResetToken> CreateAsync(PasswordResetToken passwordResetToken, CancellationToken cancellationToken = default)
    {
        dbContext.PasswordResetTokens.Add(passwordResetToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetRequiredTokenAsync(passwordResetToken.PasswordResetTokenId, cancellationToken);
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await dbContext.PasswordResetTokens
            .Include(token => token.User)
                .ThenInclude(user => user!.Role)
            .Include(token => token.User)
                .ThenInclude(user => user!.Customer)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task InvalidateActiveTokensForUserAsync(int userId, DateTimeOffset invalidatedAt, CancellationToken cancellationToken = default)
    {
        List<PasswordResetToken> activeTokens = await dbContext.PasswordResetTokens
            .Where(token => token.UserId == userId && token.UsedAt == null && token.ExpiresAt > invalidatedAt)
            .ToListAsync(cancellationToken);

        if (activeTokens.Count == 0)
        {
            return;
        }

        foreach (PasswordResetToken token in activeTokens)
        {
            token.UsedAt = invalidatedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task InvalidateOtherActiveTokensForUserAsync(
        int userId,
        int passwordResetTokenId,
        DateTimeOffset invalidatedAt,
        CancellationToken cancellationToken = default)
    {
        List<PasswordResetToken> activeTokens = await dbContext.PasswordResetTokens
            .Where(token =>
                token.UserId == userId &&
                token.PasswordResetTokenId != passwordResetTokenId &&
                token.UsedAt == null &&
                token.ExpiresAt > invalidatedAt)
            .ToListAsync(cancellationToken);

        if (activeTokens.Count == 0)
        {
            return;
        }

        foreach (PasswordResetToken token in activeTokens)
        {
            token.UsedAt = invalidatedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<PasswordResetToken> GetRequiredTokenAsync(int passwordResetTokenId, CancellationToken cancellationToken)
    {
        return await dbContext.PasswordResetTokens
            .Include(token => token.User)
                .ThenInclude(user => user!.Role)
            .Include(token => token.User)
                .ThenInclude(user => user!.Customer)
            .FirstAsync(token => token.PasswordResetTokenId == passwordResetTokenId, cancellationToken);
    }
}