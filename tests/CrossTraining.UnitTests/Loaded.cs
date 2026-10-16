using System.Reflection;
using CrossTraining.Domain.Entities;
using CrossTraining.Domain.ValueObjects;

namespace CrossTraining.UnitTests;

/// <summary>
/// Entitati in starea in care le-ar incarca EF Core din baza de date: cu id-uri si cu
/// orice nume - inclusiv drepturile si rolurile de sistem, pe care fabricile din domeniu
/// refuza, intentionat, sa le creeze.
///
/// Se construiesc prin reflexie, exact cum o face EF Core: constructorii si setterii sunt
/// privati tocmai ca restul codului sa treaca prin metodele entitatii.
/// </summary>
internal static class Loaded
{
    public static AppClaim Claim(int id, string name, bool isActive = true)
        => Hydrate<AppClaim>(("ClaimId", id), ("ClaimName", name), ("IsActive", isActive));

    /// <summary>Rolul, cu legaturile catre drepturile date deja incarcate.</summary>
    public static AppRole Role(int id, string name, bool isActive = true, params int[] claimIds)
    {
        var role = Hydrate<AppRole>(("RoleId", id), ("RoleName", name), ("IsActive", isActive));
        foreach (var claimId in claimIds)
            role.RoleClaims.Add(new RoleClaim(id, claimId));

        return role;
    }

    public static AppUser User(
        int id,
        string email = "ana@example.com",
        string name = "Ana",
        string familyName = "Popescu",
        int? roleId = 1,
        string passwordHash = "hash")
        => Hydrate<AppUser>(
            ("UserId", id),
            ("Name", name),
            ("FamilyName", familyName),
            ("Email", Email.FromStored(email)),
            ("RoleId", roleId),
            ("PasswordHash", passwordHash),
            ("IsActive", true));

    public static TrainingDomain Domain(int id, string name, bool isActive = true)
        => Hydrate<TrainingDomain>(("DomainId", id), ("DomainName", name), ("IsActive", isActive));

    public static Game Game(int id, string name, int? domainId = null, string? domainName = null)
        => Hydrate<Game>(
            ("GameId", id),
            ("GameName", name),
            ("IsActive", true),
            ("DomainId", domainId),
            ("Domain", domainId != null && domainName != null ? Domain(domainId.Value, domainName) : null));

    private static T Hydrate<T>(params (string Property, object? Value)[] values)
        where T : class
    {
        var entity = (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

        foreach (var (property, value) in values)
            typeof(T).GetProperty(property, BindingFlags.Public | BindingFlags.Instance)!.SetValue(entity, value);

        return entity;
    }
}
