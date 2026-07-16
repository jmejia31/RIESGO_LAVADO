using System.Text;

namespace RL.API.Shared.Identifiers;

public static class HashIdHelper
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int Offset = 10000;

    public static string EncodeId(long id)
    {
        if (id <= 0) return string.Empty;
        
        long n = id + Offset;
        StringBuilder sb = new();
        while (n > 0)
        {
            sb.Insert(0, Alphabet[(int)(n % Alphabet.Length)]);
            n /= Alphabet.Length;
        }
        return sb.ToString();
    }

    public static long DecodeId(string hash)
    {
        if (string.IsNullOrEmpty(hash)) return 0;

        long n = 0;
        foreach (char c in hash)
        {
            int index = Alphabet.IndexOf(c);
            if (index < 0) return 0;
            n = n * Alphabet.Length + index;
        }
        return n - Offset;
    }
}
