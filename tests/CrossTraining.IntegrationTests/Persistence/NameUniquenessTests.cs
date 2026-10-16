using CrossTraining.Domain.Entities;
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

        var name = Unique("drept");
        await using (var db = _sql.CreateDbContext())
        {
            db.Claims.Add(new AppClaim { ClaimName = name, IsActive = false });
            await db.SaveChangesAsync();
        }

        await using var second = _sql.CreateDbContext();
        second.Claims.Add(new AppClaim { ClaimName = name.ToUpperInvariant(), IsActive = true });

        // Indexul vede si randul dezactivat: altfel reactivarea lui ar produce un duplicat.
        await Should.ThrowAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task IndexulUnic_RespingeUnRolCuAcelasiNumeScrisAltfel()
    {
        Skip.IfNot(_sql.IsAvailable, _sql.SkipReason);

        var name = Unique("rol");
        await using (var db = _sql.CreateDbContext())
        {
            db.Roles.Add(new AppRole { RoleName = name, IsActive = true });
            await db.SaveChangesAsync();
        }

        await using var second = _sql.CreateDbContext();
        second.Roles.Add(new AppRole { RoleName = name.ToUpperInvariant(), IsActive = true });

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
        var claim = new AppClaim { ClaimName = Unique("drept"), IsActive = false };
        var role = new AppRole { RoleName = Unique("rol"), IsActive = false };
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
        db.Roles.Add(new AppRole { RoleName = $"lista-{marker}-activ", IsActive = true });
        db.Roles.Add(new AppRole { RoleName = $"lista-{marker}-inactiv", IsActive = false });
        await db.SaveChangesAsync();

        var result = await new AppRoleRepository(db).SearchForAdminAsync($"lista-{marker}", 1, 10);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldContain(r => !r.IsActive);
    }

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";
}
