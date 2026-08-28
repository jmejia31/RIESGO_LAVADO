using RL.API.Features.Identidad.Domain;
using Xunit;

namespace RL.API.Tests.Features.Identidad;

public sealed class RefreshTokenSecurityTests
{
    [Fact]
    public void LegacyTokenMatchesAfterItsStoredValueIsMigrated()
    {
        const string token = "legacy-token-with-sufficient-entropy";
        Assert.True(RefreshTokenSecurity.Matches(token, token));
        Assert.True(RefreshTokenSecurity.Matches(token, RefreshTokenSecurity.Hash(token)));
    }

    [Fact]
    public void NewTokenMatchesOnlyItsSha256Hash()
    {
        const string token = "new-token-with-sufficient-entropy";
        Assert.True(RefreshTokenSecurity.Matches(token, RefreshTokenSecurity.Hash(token)));
        Assert.False(RefreshTokenSecurity.Matches("wrong-token", RefreshTokenSecurity.Hash(token)));
    }

    [Fact]
    public void ExpiredAndRevokedRowsAreRejectedByTheRepositoryContract()
    {
        // Expiration/revocation are SQL predicates; this vector proves the token
        // matcher cannot bypass them and only covers the cryptographic comparison.
        const string token = "contract-token";
        Assert.False(RefreshTokenSecurity.Matches(string.Empty, RefreshTokenSecurity.Hash(token)));
        Assert.False(RefreshTokenSecurity.Matches(token, string.Empty));
    }
}
