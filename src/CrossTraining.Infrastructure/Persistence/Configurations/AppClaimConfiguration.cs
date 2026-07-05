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
        builder.Property(c => c.ClaimName).HasColumnName("ClaimName").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(c => c.IsActive).HasColumnName("IsActive");
    }
}
