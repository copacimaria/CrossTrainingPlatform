using CrossTraining.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CrossTraining.IntegrationTests.Persistence;

/// <summary>
/// Numele drepturilor si al rolurilor sunt unice, iar verificarea din handlere foloseste
/// aceleasi reguli de comparatie ca indexul. Ambele depind de colatia SQL Server
/// (fara diferenta intre litere mari si mici), deci nu se pot verifica cu mock-uri.
/// </summary>
[Collection(SqlServerCollection.Name)]
public class NameUniquenessTests
{
    private readonly SqlServerFixture _sql;

    public NameUniquenessTests(SqlServerFixture sql)
    {
        _sql = sql;
    }

    [SkippableFact]
    public async Task IndexulUnic_RespingeUnDreptCuAcelasiNumeScrisAltfel()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        var name = TestData.Unique("drept");
        await using (var db = _sql.CreateDbContext())
        {
            db.Claims.Add(TestData.Claim(name, isActive: false));
            await db.SaveChangesAsync();
        }

        await using var second = _sql.CreateDbContext();
        second.Claims.Add(TestData.Claim(name.ToUpperInvariant()));

        // Indexul vede si randul dezactivat: altfel reactivarea lui ar produce un duplicat.
        await Should.ThrowAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task IndexulUnic_RespingeUnRolCuAcelasiNumeScrisAltfel()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        var name = TestData.Unique("rol");
        await using (var db = _sql.CreateDbContext())
        {
            db.Roles.Add(TestData.Role(name));
            await db.SaveChangesAsync();
        }

        await using var second = _sql.CreateDbContext();
        second.Roles.Add(TestData.Role(name.ToUpperInvariant()));

        await Should.ThrowAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task CautareaDupaNume_GasesteSiRandurileDezactivate_FaraDiferentaDeLitere()
    {
        // Daca verificarea din handler ar compara altfel decat indexul, ar spune "liber"
        // pentru un nume pe care indexul il respinge, iar utilizatorul ar primi o eroare
        // 500 in loc de mesaj.
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var claim = TestData.Claim(TestData.Unique("drept"), isActive: false);
        var role = TestData.Role(TestData.Unique("rol"), isActive: false);
        db.Claims.Add(claim);
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var foundClaim = await new AppClaimRepository(db).GetByNameForAdminAsync(claim.ClaimName.ToUpperInvariant());
        var foundRole = await new AppRoleRepository(db).GetByNameForAdminAsync(role.RoleName.ToUpperInvariant());

        foundClaim.ShouldNotBeNull().ClaimId.ShouldBe(claim.ClaimId);
        foundRole.ShouldNotBeNull().RoleId.ShouldBe(role.RoleId);
    }

    [SkippableFact]
    public async Task ListaDeRoluri_ArataSiRolurileDezactivate()
    {
        // Inainte, lista le ascundea: un rol dezactivat nu mai putea fi gasit, deci nici
        // reactivat.
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        await using var db = _sql.CreateDbContext();
        var marker = Guid.NewGuid().ToString("N")[..8];
        db.Roles.Add(TestData.Role($"lista-{marker}-activ"));
        db.Roles.Add(TestData.Role($"lista-{marker}-inactiv", isActive: false));
        await db.SaveChangesAsync();

        var result = await new AppRoleRepository(db).SearchForAdminAsync($"lista-{marker}", 1, 10);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldContain(r => !r.IsActive);
    }
}
