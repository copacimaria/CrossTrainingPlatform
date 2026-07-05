using CrossTrainingPlatform.Models.UserManagementModels;
using Microsoft.AspNetCore.Identity;

namespace CrossTrainingPlatform.Services.Security
{
    /// <summary>
    /// Centralizeaza hashing-ul si verificarea parolelor.
    /// Foloseste PasswordHasher din ASP.NET Core Identity (PBKDF2 cu salt per parola).
    /// Parolele NU se mai stocheaza niciodata in text clar.
    /// </summary>
    public class PasswordService
    {
        private readonly PasswordHasher<AppUser> _hasher = new();

        /// <summary>Genereaza hash-ul unei parole in text clar (pentru inregistrare / schimbare parola).</summary>
        public string Hash(AppUser user, string plainPassword)
            => _hasher.HashPassword(user, plainPassword);

        /// <summary>
        /// Verifica o parola introdusa fata de hash-ul stocat.
        /// Returneaza true daca parola e corecta.
        /// </summary>
        public bool Verify(AppUser user, string storedHash, string plainPassword)
        {
            var result = _hasher.VerifyHashedPassword(user, storedHash, plainPassword);
            return result == PasswordVerificationResult.Success
                || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
