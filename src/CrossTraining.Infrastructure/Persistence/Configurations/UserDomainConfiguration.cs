using CrossTraining.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossTraining.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapare pe tabelul nou dbo.UserDomain (many-to-many user &lt;-&gt; domenii).
/// Inlocuieste vechea coloana JSON DomainList din AppUser.
/// </summary>
public class UserDomainConfiguration : IEntityTypeConfiguration<UserDomain>
{
    public void Configure(EntityTypeBuilder<UserDomain> builder)
    {
        builder.ToTable("UserDomain");
        builder.HasKey(ud => new { ud.UserId, ud.DomainId });
        builder.HasIndex(ud => ud.DomainId).HasDatabaseName("IX_UserDomain_DomainId");

        // Filtru pereche cu cel de pe AppUser si Domain, din acelasi motiv ca la RoleClaim.
        builder.HasQueryFilter(ud => ud.User!.IsActive && ud.Domain!.IsActive);

        builder.Property(ud => ud.UserId).HasColumnName("UserId");
        builder.Property(ud => ud.DomainId).HasColumnName("DomainId");

        builder.HasOne(ud => ud.User)
               .WithMany(u => u.UserDomains)
               .HasForeignKey(ud => ud.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ud => ud.Domain)
               .WithMany(d => d.UserDomains)
               .HasForeignKey(ud => ud.DomainId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
