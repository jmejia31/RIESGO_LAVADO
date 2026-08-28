namespace RL.API.Features.Identidad.Domain;

public static class RefreshTokenMigrationPolicy
{
    public const string LegacyPredicate = "RFT_TOKEN IS NOT NULL AND (LENGTH(RFT_TOKEN) <> 64 OR NOT REGEXP_LIKE(RFT_TOKEN, '^[0-9A-Fa-f]{64}$'))";
    public const string HashPredicate = "LENGTH(RFT_TOKEN) = 64 AND REGEXP_LIKE(RFT_TOKEN, '^[0-9A-Fa-f]{64}$')";

    public static bool IsLegacy(string value) =>
        !string.IsNullOrWhiteSpace(value) && !RefreshTokenSecurity.IsSha256Hash(value);

    public static bool CanCommit(long inputLegacy, long migrated, long postTotal, long postHashed, long postRequiresMigration, long invalidFormat) =>
        inputLegacy > 0 && migrated == inputLegacy && postTotal >= inputLegacy && postHashed == postTotal && postRequiresMigration == 0 && invalidFormat == 0;
}
