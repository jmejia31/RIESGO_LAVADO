using System.Security.Cryptography;
using System.Text;

namespace RL.API.Features.Identidad.Domain;

public static class RefreshTokenSecurity
{
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token ?? string.Empty)));

    public static bool IsSha256Hash(string value) =>
        value?.Length == 64 && value.All(static c => c is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f');

    public static bool Matches(string presentedToken, string storedValue)
    {
        if (string.IsNullOrWhiteSpace(presentedToken) || string.IsNullOrWhiteSpace(storedValue)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(IsSha256Hash(storedValue) ? Hash(presentedToken) : presentedToken),
            Encoding.ASCII.GetBytes(storedValue));
    }
}
