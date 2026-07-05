using CrossTraining.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossTraining.Infrastructure.Persistence.Configurations;

/// <summary>Mapare pe tabela pivot existenta dbo.AppClaim_AppRole.</summary>
public class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
{
    public void Configure(EntityTypeBuilder<RoleClaim> builder)
    {
        builder.ToTable("AppClaim_AppRole");
        builder.HasKey(rc => new { rc.RoleId, rc.ClaimId });

        builder.Property(rc => rc.RoleId).HasColumnName("RoleId");
        builder.Property(rc => rc.ClaimId).HasColumnName("ClaimId");

        builder.HasOne(rc => rc.Role)
               .WithMany(r => r.RoleClaims)
               .HasForeignKey(rc => rc.RoleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rc => rc.Claim)
               .WithMany(c => c.RoleClaims)
               .HasForeignKey(rc => rc.ClaimId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
