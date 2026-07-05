using CrossTraining.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossTraining.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("AppUser");
        builder.HasKey(u => u.UserId);

        builder.Property(u => u.UserId).HasColumnName("UserId");
        builder.Property(u => u.Name).HasColumnName("Name").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(u => u.FamilyName).HasColumnName("FamilyName").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(u => u.Email).HasColumnName("Email").HasColumnType("nvarchar(max)").IsRequired();

        // Hash-ul parolei (Argon2id) este stocat in coloana PasswordHash.
        builder.Property(u => u.PasswordHash).HasColumnName("PasswordHash").HasColumnType("nvarchar(256)").IsRequired();

        builder.Property(u => u.CreationDate).HasColumnName("CreationDate");
        builder.Property(u => u.IsActive).HasColumnName("isActive");
        builder.Property(u => u.RoleId).HasColumnName("RoleId");

        builder.HasOne(u => u.Role)
               .WithMany(r => r.Users)
               .HasForeignKey(u => u.RoleId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
