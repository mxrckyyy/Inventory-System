using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Inventory_System.Services;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private const KeyDerivationPrf Prf = KeyDerivationPrf.HMACSHA256;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var subKey = KeyDerivation.Pbkdf2(password, salt, Prf, Iterations, KeySize);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(subKey)}";
    }

    public static bool Verify(string hash, string password)
    {
        if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(password)) return false;

        var parts = hash.Split(':');
        if (parts.Length != 2) return false;

        byte[] salt;
        byte[] storedSubKey;
        try
        {
            salt = Convert.FromBase64String(parts[0]);
            storedSubKey = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        var candidateSubKey = KeyDerivation.Pbkdf2(password, salt, Prf, Iterations, storedSubKey.Length);
        return CryptographicOperations.FixedTimeEquals(candidateSubKey, storedSubKey);
    }
}