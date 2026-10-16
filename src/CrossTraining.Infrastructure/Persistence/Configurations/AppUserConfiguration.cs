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
        builder.Property(u => u.Name).HasColumnName("Name").HasMaxLength(100).IsRequired();
        builder.Property(u => u.FamilyName).HasColumnName("FamilyName").HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasColumnName("Email").HasMaxLength(256).IsRequired();

        // Hash-ul parolei (Argon2id, format PHC) este stocat in coloana PasswordHash.
        builder.Property(u => u.PasswordHash).HasColumnName("PasswordHash").HasMaxLength(256).IsRequired();

        builder.Property(u => u.CreatedAt).HasColumnName("CreationDate");
        builder.Property(u => u.IsActive).HasColumnName("isActive");
        builder.Property(u => u.RoleId).HasColumnName("RoleId");

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UX_AppUser_Email");
        builder.HasIndex(u => u.RoleId).HasDatabaseName("IX_AppUser_RoleId");

        // SetNull, ca in schema: stergerea unui rol lasa utilizatorii fara rol,
        // nu blocheaza stergerea. Configurarea spunea Restrict, ceea ce diverga de
        // ce face de fapt baza de date.
        builder.HasOne(u => u.Role)
               .WithMany(r => r.Users)
               .HasForeignKey(u => u.RoleId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
