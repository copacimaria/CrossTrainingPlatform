using CrossTraining.Application.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CrossTraining.IntegrationTests.Persistence;

/// <summary>
/// Autorizarea pe drepturi functioneaza doar daca drepturile exista in baza si sunt
/// acordate rolului Admin. Migrarea SeedPermissions face asta; testele de aici verifica
/// rezultatul ei pe o baza creata din migrari, nu presupun ca a mers.
/// </summary>
[Collection(SqlServerCollection.Name)]
public class PermissionSeedTests
{
    private readonly SqlServerFixture _sql;

    public PermissionSeedTests(SqlServerFixture sql)
    {
        _sql = sql;
    }

    [SkippableFact]
    public async Task ToateDrepturileDinCod_ExistaInBazaDeDate()
    {
        // Daca cineva adauga o constanta in Permissions fara sa scrie migrarea,
        // endpointul protejat cu ea devine inaccesibil pentru toata lumea - iar asta
        // se observa greu in productie.
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();

        var stored = await db.Claims
            .IgnoreQueryFilters()
            .Select(c => c.ClaimName)
            .ToListAsync();

        foreach (var permission in Permissions.All)
            stored.ShouldContain(permission, $"dreptul {permission} nu a fost sadit in baza");
    }

    [SkippableFact]
    public async Task RolulAdmin_AreToateDrepturile()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();

        var adminPermissions = await db.Claims
            .Where(c => c.RoleClaims.Any(rc => rc.Role!.RoleName == "Admin"))
            .Select(c => c.ClaimName)
            .ToListAsync();

        foreach (var permission in Permissions.All)
            adminPermissions.ShouldContain(permission, $"rolul Admin nu are dreptul {permission}");
    }

    [SkippableFact]
    public async Task RolurileDeBaza_ExistaCuIdurileAsteptate()
    {
        // AuthService atribuie RoleId = 1 conturilor noi, deci id-ul nu este arbitrar.
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();

        var roles = await db.Roles
            .IgnoreQueryFilters()
            .Where(r => r.RoleId <= 2)
            .ToDictionaryAsync(r => r.RoleId, r => r.RoleName);

        roles.ShouldContainKeyAndValue(1, "User");
        roles.ShouldContainKeyAndValue(2, "Admin");
    }
}
