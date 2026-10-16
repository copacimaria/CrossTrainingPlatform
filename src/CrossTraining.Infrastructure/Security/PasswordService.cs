using System.Security.Cryptography;
using System.Text;
using CrossTraining.Application.Abstractions.Security;
using CrossTraining.Domain.Entities;
using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;

namespace CrossTraining.Infrastructure.Security;

/// <summary>
/// Hashing de parole cu Argon2id (castigatorul Password Hashing Competition).
/// Rezistent la atacuri GPU si side-channel. Fiecare parola primeste un salt unic.
/// Formatul rezultat este PHC standard ($argon2id$v=19$m=...,t=...,p=...$salt$hash),
/// deci hash-ul contine in el toti parametrii necesari verificarii.
/// </summary>
public class PasswordService : IPasswordService
{
    // Parametri Argon2id. Pot fi crescuti in timp pe masura ce hardware-ul devine mai rapid.
    private const int SaltLength = 16;      // 128 biti
    private const int HashLength = 32;      // 256 biti
    private const int TimeCost = 3;         // numar de iteratii
    private const int MemoryCostKb = 65536; // 64 MB
    private const int Parallelism = 2;      // fire de executie

    public string Hash(AppUser user, string plainPassword)
    {
        var salt = new byte[SaltLength];
        RandomNumberGenerator.Fill(salt);

        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing, // Argon2id
            Version = Argon2Version.Nineteen,
            Password = Encoding.UTF8.GetBytes(plainPassword),
            Salt = salt,
            TimeCost = TimeCost,
            MemoryCost = MemoryCostKb,
            Lanes = Parallelism,
            Threads = Parallelism,
            HashLength = HashLength
        };

        using var argon2 = new Argon2(config);
        using SecureArray<byte> hash = argon2.Hash();
        return config.EncodeString(hash.Buffer); // string PHC, gata de stocat
    }

    public bool Verify(AppUser user, string storedHash, string plainPassword)
    {
        // Hash gol sau invalid (ex: cont vechi) -> tratat ca parola gresita, fara exceptie.
        if (string.IsNullOrWhiteSpace(storedHash))
            return false;

        try
        {
            return Argon2.Verify(storedHash, Encoding.UTF8.GetBytes(plainPassword));
        }
        catch
        {
            return false;
        }
    }
}
