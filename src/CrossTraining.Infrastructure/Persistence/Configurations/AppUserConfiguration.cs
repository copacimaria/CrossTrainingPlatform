using CrossTraining.Domain.Entities;
using CrossTraining.Domain.ValueObjects;
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
        // Email este value object in domeniu si string in baza: coloana ramane aceeasi,
        // conversia se face la citire si la scriere. Valorile citite nu se revalideaza.
        builder.Property(u => u.Email)
               .HasColumnName("Email")
               .HasMaxLength(Email.MaxLength)
               .IsRequired()
               .HasConversion(email => email.Value, value => Email.FromStored(value));

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
