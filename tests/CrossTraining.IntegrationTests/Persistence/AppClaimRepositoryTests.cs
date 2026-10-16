using CrossTraining.Domain.Entities;
using CrossTraining.Infrastructure.Persistence;
using CrossTraining.Infrastructure.Repositories;

namespace CrossTraining.IntegrationTests.Persistence;

/// <summary>
/// Interogarile de cautare nu pot fi verificate cu mock-uri: traducerea LINQ -> SQL
/// si semantica LIKE apartin furnizorului de baza de date. Testele merg pe SQL Server real.
/// </summary>
[Collection(SqlServerCollection.Name)]
public class AppClaimRepositoryTests
{
    private readonly SqlServerFixture _sql;

    public AppClaimRepositoryTests(SqlServerFixture sql)
    {
        _sql = sql;
    }

    [SkippableFact]
    public async Task SearchAvailableForRoleAsync_ExcludeClaimurileDejaAtasateRolului()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var (roleId, atasat, disponibil) = await SeedRoleWithOneClaimAsync(db, "atasat", "disponibil");

        var results = await new AppClaimRepository(db).SearchAvailableForRoleAsync(roleId, null, 20);

        var ids = results.Select(c => c.ClaimId).ToList();
        ids.ShouldContain(disponibil);
        ids.ShouldNotContain(atasat);
    }

    [SkippableFact]
    public async Task SearchAvailableForRoleAsync_ExcludeClaimurileInactive()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var role = await AddRoleAsync(db);
        var activ = await AddClaimAsync(db, Unique("activ"), isActive: true);
        var inactiv = await AddClaimAsync(db, Unique("inactiv"), isActive: false);

        var results = await new AppClaimRepository(db).SearchAvailableForRoleAsync(role, null, 50);

        var ids = results.Select(c => c.ClaimId).ToList();
        ids.ShouldContain(activ);
        ids.ShouldNotContain(inactiv);
    }

    [SkippableFact]
    public async Task SearchAvailableForRoleAsync_FiltreazaDupaTermen()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var role = await AddRoleAsync(db);
        var marker = Guid.NewGuid().ToString("N")[..8];
        var potrivit = await AddClaimAsync(db, $"jocuri-{marker}-editare");
        var nepotrivit = await AddClaimAsync(db, $"roluri-{marker}-editare");

        var results = await new AppClaimRepository(db)
            .SearchAvailableForRoleAsync(role, $"jocuri-{marker}", 20);

        var ids = results.Select(c => c.ClaimId).ToList();
        ids.ShouldContain(potrivit);
        ids.ShouldNotContain(nepotrivit);
    }

    [SkippableFact]
    public async Task SearchAvailableForRoleAsync_TrateazaProcentulCaTextNuCaWildcard()
    {
        // Regresie: fara escapare, un "%" introdus de utilizator devine wildcard si
        // interogarea intoarce tot tabelul in loc de nimic.
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var role = await AddRoleAsync(db);
        await AddClaimAsync(db, Unique("oarecare"));

        var results = await new AppClaimRepository(db).SearchAvailableForRoleAsync(role, "%", 50);

        results.ShouldBeEmpty();
    }

    [SkippableFact]
    public async Task SearchAvailableForRoleAsync_TrateazaUnderscoreCaTextNuCaWildcard()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var role = await AddRoleAsync(db);
        var marker = Guid.NewGuid().ToString("N")[..8];
        var cuUnderscore = await AddClaimAsync(db, $"a_b-{marker}");
        var faraUnderscore = await AddClaimAsync(db, $"axb-{marker}");

        // "_" in LIKE inseamna "orice caracter"; escapat, inseamna chiar underscore.
        var results = await new AppClaimRepository(db)
            .SearchAvailableForRoleAsync(role, $"a_b-{marker}", 20);

        var ids = results.Select(c => c.ClaimId).ToList();
        ids.ShouldContain(cuUnderscore);
        ids.ShouldNotContain(faraUnderscore);
    }

    [SkippableFact]
    public async Task SearchAvailableForRoleAsync_RespectaLimita()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var role = await AddRoleAsync(db);
        var marker = Guid.NewGuid().ToString("N")[..8];
        for (int i = 0; i < 5; i++)
            await AddClaimAsync(db, $"lot-{marker}-{i}");

        var results = await new AppClaimRepository(db)
            .SearchAvailableForRoleAsync(role, $"lot-{marker}", limit: 3);

        results.Count.ShouldBe(3);
    }

    // ------------------- helperi de seed -------------------

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static async Task<int> AddRoleAsync(AppDbContext db)
    {
        var role = new AppRole { RoleName = Unique("rol"), IsActive = true };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role.RoleId;
    }

    private static async Task<int> AddClaimAsync(AppDbContext db, string name, bool isActive = true)
    {
        var claim = new AppClaim { ClaimName = name, IsActive = isActive };
        db.Claims.Add(claim);
        await db.SaveChangesAsync();
        return claim.ClaimId;
    }

    private static async Task<(int RoleId, int Attached, int Available)> SeedRoleWithOneClaimAsync(
        AppDbContext db, string attachedName, string availableName)
    {
        var roleId = await AddRoleAsync(db);
        var attached = await AddClaimAsync(db, Unique(attachedName));
        var available = await AddClaimAsync(db, Unique(availableName));

        db.RoleClaims.Add(new RoleClaim { RoleId = roleId, ClaimId = attached });
        await db.SaveChangesAsync();

        return (roleId, attached, available);
    }
}
