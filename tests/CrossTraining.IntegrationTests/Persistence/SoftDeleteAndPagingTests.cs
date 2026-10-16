using CrossTraining.Domain.Entities;
using CrossTraining.Infrastructure.Persistence;
using CrossTraining.Infrastructure.Repositories;

namespace CrossTraining.IntegrationTests.Persistence;

/// <summary>
/// Filtrul global de soft-delete si paginarea la nivel de baza de date sunt schimbari
/// care nu se pot verifica cu mock-uri: prima e o traducere de model EF in SQL, a doua
/// depinde de <c>COUNT</c> si <c>OFFSET/FETCH</c>.
/// </summary>
[Collection(SqlServerCollection.Name)]
public class SoftDeleteAndPagingTests
{
    private readonly SqlServerFixture _sql;

    public SoftDeleteAndPagingTests(SqlServerFixture sql)
    {
        _sql = sql;
    }

    // ------------------- FILTRU GLOBAL -------------------

    [SkippableFact]
    public async Task InterogarileObisnuite_NuVadRandurileDezactivate()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var activ = await AddDomainAsync(db, isActive: true);
        var inactiv = await AddDomainAsync(db, isActive: false);

        var repository = new DomainRepository(db);
        var ids = (await repository.GetActiveAsync()).Select(d => d.DomainId).ToList();

        ids.ShouldContain(activ);
        ids.ShouldNotContain(inactiv);
    }

    [SkippableFact]
    public async Task InterogarileDeAdministrare_VadSiRandurileDezactivate()
    {
        // Altfel un domeniu dezactivat nu ar mai putea fi reactivat niciodata.
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var inactiv = await AddDomainAsync(db, isActive: false);

        var repository = new DomainRepository(db);

        (await repository.GetAllForAdminAsync()).Select(d => d.DomainId).ShouldContain(inactiv);
        (await repository.GetForAdminAsync(inactiv)).ShouldNotBeNull();
    }

    [SkippableFact]
    public async Task UtilizatorDezactivat_NuMaiPoateFiGasitLaLogin()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var email = $"dezactivat-{Guid.NewGuid():N}@test.local";
        db.Users.Add(new AppUser
        {
            Name = "X",
            FamilyName = "Y",
            Email = email,
            PasswordHash = "$argon2id$fictiv",
            IsActive = false
        });
        await db.SaveChangesAsync();

        var repository = new AppUserRepository(db);

        (await repository.GetByEmailAsync(email)).ShouldBeNull();
    }

    [SkippableFact]
    public async Task EmailExistsAsync_VedeSiConturileDezactivate()
    {
        // Emailul are index unic: daca verificarea de la inregistrare ar folosi
        // interogarea filtrata, adresa ar parea libera si inserarea ar pica pe index.
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var email = $"ocupat-{Guid.NewGuid():N}@test.local";
        db.Users.Add(new AppUser
        {
            Name = "X",
            FamilyName = "Y",
            Email = email,
            PasswordHash = "$argon2id$fictiv",
            IsActive = false
        });
        await db.SaveChangesAsync();

        (await new AppUserRepository(db).EmailExistsAsync(email)).ShouldBeTrue();
    }

    [SkippableFact]
    public async Task DrepturileRolului_NuContinNulluriPentruDrepturiDezactivate()
    {
        // Proiectia mergea prin tabelul pivot: un drept dezactivat era filtrat, dar
        // randul de legatura ramanea, deci in lista aparea null.
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var role = new AppRole { RoleName = Unique("rol"), IsActive = true };
        var activ = new AppClaim { ClaimName = Unique("activ"), IsActive = true };
        var inactiv = new AppClaim { ClaimName = Unique("inactiv"), IsActive = false };
        db.Roles.Add(role);
        db.Claims.AddRange(activ, inactiv);
        await db.SaveChangesAsync();

        db.RoleClaims.Add(new RoleClaim { RoleId = role.RoleId, ClaimId = activ.ClaimId });
        db.RoleClaims.Add(new RoleClaim { RoleId = role.RoleId, ClaimId = inactiv.ClaimId });
        await db.SaveChangesAsync();

        var claims = await new AppRoleRepository(db).GetClaimsForRoleAsync(role.RoleId);

        claims.ShouldNotContain((AppClaim?)null);
        claims.Select(c => c.ClaimId).ShouldBe([activ.ClaimId]);
    }

    // ------------------- PAGINARE IN BAZA DE DATE -------------------

    [SkippableFact]
    public async Task Paginarea_IntoarceDoarPaginaCerutaDarNumaraTot()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var marker = Guid.NewGuid().ToString("N")[..8];
        for (int i = 0; i < 7; i++)
            db.Claims.Add(new AppClaim { ClaimName = $"pag-{marker}-{i:D2}", IsActive = true });
        await db.SaveChangesAsync();

        var repository = new AppClaimRepository(db);

        var first = await repository.SearchForAdminAsync($"pag-{marker}", page: 1, pageSize: 3);
        var last = await repository.SearchForAdminAsync($"pag-{marker}", page: 3, pageSize: 3);

        first.Items.Count.ShouldBe(3);
        first.TotalCount.ShouldBe(7);
        first.TotalPages.ShouldBe(3);

        // Ultima pagina e partiala, dar totalul ramane acelasi.
        last.Items.Count.ShouldBe(1);
        last.TotalCount.ShouldBe(7);

        first.Items.Select(c => c.ClaimId).ShouldNotBe(last.Items.Select(c => c.ClaimId));
    }

    [SkippableFact]
    public async Task PaginareaDeAdministrare_IncludeSiDrepturileDezactivate()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var marker = Guid.NewGuid().ToString("N")[..8];
        db.Claims.Add(new AppClaim { ClaimName = $"adm-{marker}-activ", IsActive = true });
        db.Claims.Add(new AppClaim { ClaimName = $"adm-{marker}-inactiv", IsActive = false });
        await db.SaveChangesAsync();

        var result = await new AppClaimRepository(db).SearchForAdminAsync($"adm-{marker}", 1, 10);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldContain(c => !c.IsActive);
    }

    // ------------------- helperi -------------------

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private static async Task<int> AddDomainAsync(AppDbContext db, bool isActive)
    {
        var domain = new TrainingDomain { DomainName = Unique("domeniu"), IsActive = isActive };
        db.Domains.Add(domain);
        await db.SaveChangesAsync();
        return domain.DomainId;
    }
}
