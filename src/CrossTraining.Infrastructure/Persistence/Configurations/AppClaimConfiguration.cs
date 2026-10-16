using CrossTraining.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossTraining.Infrastructure.Persistence.Configurations;

public class AppClaimConfiguration : IEntityTypeConfiguration<AppClaim>
{
    public void Configure(EntityTypeBuilder<AppClaim> builder)
    {
        builder.ToTable("AppClaim");
        builder.HasKey(c => c.ClaimId);

        builder.Property(c => c.ClaimId).HasColumnName("ClaimId");
        builder.Property(c => c.ClaimName).HasColumnName("ClaimName").HasMaxLength(100).IsRequired();
        builder.Property(c => c.IsActive).HasColumnName("IsActive");

        // Autorizarea gaseste drepturile dupa nume, deci numele trebuie sa fie unic. Fara
        // index, un al doilea "roles.manage" ar putea fi creat si acordat pe langa cel real.
        // Indexul cuprinde si drepturile dezactivate: reactivarea nu trebuie sa produca
        // un duplicat.
        builder.HasIndex(c => c.ClaimName).IsUnique().HasDatabaseName("UX_AppClaim_ClaimName");
    }
}
