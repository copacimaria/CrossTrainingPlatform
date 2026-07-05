using CrossTraining.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossTraining.Infrastructure.Persistence.Configurations;

public class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.ToTable("AppRole");
        builder.HasKey(r => r.RoleId);

        builder.Property(r => r.RoleId).HasColumnName("RoleId");
        builder.Property(r => r.RoleName).HasColumnName("RoleName").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(r => r.IsActive).HasColumnName("IsActive");
    }
}
