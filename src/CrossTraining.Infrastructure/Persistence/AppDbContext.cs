using System.Linq.Expressions;
using System.Reflection;
using CrossTraining.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrossTraining.Infrastructure.Persistence;

/// <summary>
/// DbContext EF Core mapat pe schema CrossTrainingPlatform.
/// Configurarile de mapare sunt in folderul Configurations (Fluent API).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<TrainingDomain> Domains => Set<TrainingDomain>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<AppClaim> Claims => Set<AppClaim>();
    public DbSet<UserDomain> UserDomains => Set<UserDomain>();
    public DbSet<RoleClaim> RoleClaims => Set<RoleClaim>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ApplySoftDeleteFilters(modelBuilder);
    }

    /// <summary>
    /// Randurile dezactivate dispar din orice interogare, implicit.
    ///
    /// Ecranele de administrare, care trebuie sa vada si randurile dezactivate ca sa le
    /// poata reactiva, cer explicit <c>IgnoreQueryFilters()</c>. Asa, a uita filtrul
    /// intr-o interogare noua nu mai este un bug de securitate, ci cel mult o omisiune
    /// vizibila intr-un ecran de admin.
    /// </summary>
    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var isActive = Expression.Property(
                Expression.Convert(parameter, typeof(ISoftDeletable)),
                nameof(ISoftDeletable.IsActive));

            modelBuilder.Entity(entityType.ClrType)
                        .HasQueryFilter(Expression.Lambda(isActive, parameter));
        }
    }
}
