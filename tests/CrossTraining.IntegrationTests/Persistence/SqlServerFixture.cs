using System.Globalization;
using CrossTraining.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CrossTraining.IntegrationTests.Persistence;

/// <summary>
/// Creeaza o baza de date de test separata si aplica pe ea migrarile EF Core.
///
///
/// Nu se foloseste Testcontainers: masina de dezvoltare are deja SQL Server Express,
/// iar in CI exista un service container. Asa, testele ruleaza fara Docker si fara sa
/// descarce o imagine de un giga la fiecare build.
///
/// Daca nu se poate ajunge la niciun SQL Server, <see cref="IsAvailable"/> ramane fals
/// si testele care depind de el se marcheaza ca sarite - nu ca picate.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    /// <summary>Baza de date proprie testelor. Nu se atinge niciodata cea de dezvoltare.</summary>
    private const string TestDatabaseName = "CrossTrainingPlatform_Tests";

    private const string DefaultServer =
        @"Server=localhost\SQLEXPRESS;Trusted_Connection=True;TrustServerCertificate=True";

    public bool IsAvailable { get; private set; }

    public string? SkipReason { get; private set; }

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // In CI, service container-ul isi da propriul connection string prin mediu.
        var serverConnectionString =
            Environment.GetEnvironmentVariable("CROSSTRAINING_TEST_SQL") ?? DefaultServer;

        var masterBuilder = new SqlConnectionStringBuilder(serverConnectionString)
        {
            InitialCatalog = "master",
            ConnectTimeout = 5
        };

        try
        {
            await using var master = new SqlConnection(masterBuilder.ConnectionString);
            await master.OpenAsync();

            await ExecuteAsync(master, string.Create(
                CultureInfo.InvariantCulture,
                $"IF DB_ID('{TestDatabaseName}') IS NULL CREATE DATABASE [{TestDatabaseName}];"));
        }
        catch (SqlException ex)
        {
            SkipReason =
                $"Niciun SQL Server accesibil ({ex.Message.Split('\n')[0].Trim()}). " +
                "Seteaza CROSSTRAINING_TEST_SQL ca sa rulezi si testele de persistenta.";
            return;
        }

        var testBuilder = new SqlConnectionStringBuilder(serverConnectionString)
        {
            InitialCatalog = TestDatabaseName,
            ConnectTimeout = 5
        };
        ConnectionString = testBuilder.ConnectionString;

        // Schema vine din migrari, nu din scriptul de dezvoltare: acesta este drumul pe
        // care il parcurge si productia, iar migrarea SeedPermissions aduce rolurile de
        // baza si drepturile de care depinde autorizarea.
        await using (var db = CreateDbContext())
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        IsAvailable = true;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Un DbContext nou, catre baza de test. Apelantul il elibereaza.</summary>
    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

}
