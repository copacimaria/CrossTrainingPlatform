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
        builder.Property(r => r.RoleName).HasColumnName("RoleName").HasMaxLength(100).IsRequired();
        builder.Property(r => r.IsActive).HasColumnName("IsActive");

        // Rolurile de sistem (User, Admin) sunt recunoscute dupa nume, deci numele trebuie
        // sa fie unic - inclusiv fata de rolurile dezactivate.
        builder.HasIndex(r => r.RoleName).IsUnique().HasDatabaseName("UX_AppRole_RoleName");
    }
}
