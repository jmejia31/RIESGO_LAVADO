using RL.API.Features.Identidad.Domain;
using Xunit;

namespace RL.API.Tests.Features.Identidad;

public sealed class RefreshTokenMigrationPolicyTests
{
    [Fact]
    public void UsesTheBackendHashAndLegacyPredicateIsIdempotent()
    {
        const string token = "legacy-token-for-migration";
        var hash = RefreshTokenSecurity.Hash(token);
        Assert.Equal(64, hash.Length);
        Assert.True(RefreshTokenSecurity.IsSha256Hash(hash));
        Assert.True(RefreshTokenMigrationPolicy.IsLegacy(token));
        Assert.False(RefreshTokenMigrationPolicy.IsLegacy(hash));
    }

    [Fact]
    public void CommitRequiresExactCounts()
    {
        Assert.True(RefreshTokenMigrationPolicy.CanCommit(593, 593, 593, 593, 0, 0));
        Assert.False(RefreshTokenMigrationPolicy.CanCommit(593, 592, 593, 593, 0, 0));
        Assert.False(RefreshTokenMigrationPolicy.CanCommit(593, 593, 594, 593, 0, 0));
        Assert.False(RefreshTokenMigrationPolicy.CanCommit(593, 593, 593, 592, 0, 0));
    }
}
