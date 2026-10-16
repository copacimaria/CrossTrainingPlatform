using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CrossTraining.Infrastructure.Persistence;

/// <summary>
/// Folosit doar de uneltele EF Core (<c>dotnet ef</c>), la generarea migrarilor.
///
/// Fara el, uneltele ar trebui sa porneasca aplicatia web ca sa obtina un DbContext,
/// ceea ce ar insemna ca generarea unei migrari depinde de configuratia de rulare
/// (user-secrets, mediu). Aici e nevoie doar de furnizorul de baza de date, ca sa se
/// stie ce SQL sa se genereze - conexiunea nu se deschide niciodata.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DesignTimeConnectionString =
        @"Server=(localdb)\design-time;Database=CrossTrainingPlatform;Trusted_Connection=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CROSSTRAINING_MIGRATIONS_SQL")
            ?? DesignTimeConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
