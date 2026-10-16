using CrossTraining.Domain.Entities;
using CrossTraining.Domain.ValueObjects;

namespace CrossTraining.IntegrationTests.Persistence;

/// <summary>
/// Date de test create prin metodele din domeniu, la fel ca in aplicatie: daca o regula a
/// entitatii se schimba, si datele de test o respecta.
/// </summary>
internal static class TestData
{
    /// <summary>Rolul "User", sadit de migrarea SeedPermissions in orice baza de test.</summary>
    public const int UserRoleId = 1;

    public static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    public static AppClaim Claim(string name, bool isActive = true)
    {
        var claim = AppClaim.Create(name).Value;
        if (!isActive)
            claim.Deactivate().Succeeded.ShouldBeTrue();

        return claim;
    }

    public static AppRole Role(string name, bool isActive = true)
    {
        var role = AppRole.Create(name).Value;
        if (!isActive)
            role.Deactivate().Succeeded.ShouldBeTrue();

        return role;
    }

    public static TrainingDomain Domain(string name, bool isActive = true)
        => TrainingDomain.Create(name, isActive).Value;

    public static Game Game(string name, int? domainId, string? description = null)
        => global::CrossTraining.Domain.Entities.Game.Create(name, description, downloadLink: null, domainId, DateTime.UtcNow).Value;

    public static AppUser User(
        string email,
        int roleId = UserRoleId,
        bool isActive = true,
        string passwordHash = "$argon2id$fictiv")
    {
        var user = AppUser.Register("Test", "Utilizator", Email.Create(email).Value, roleId, DateTime.UtcNow).Value;
        user.SetPasswordHash(passwordHash);
        if (!isActive)
            user.Deactivate();

        return user;
    }
}
