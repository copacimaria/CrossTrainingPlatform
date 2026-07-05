using CrossTraining.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CrossTraining.Infrastructure.Persistence;

/// <summary>
/// DbContext EF Core mapat Database-First pe schema existenta CrossTrainingPlatform.
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
    }
}
