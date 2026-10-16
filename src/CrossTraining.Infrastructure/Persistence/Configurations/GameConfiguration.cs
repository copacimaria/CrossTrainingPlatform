using CrossTraining.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossTraining.Infrastructure.Persistence.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("Game");
        builder.HasKey(g => g.GameId);

        builder.Property(g => g.GameId).HasColumnName("GameId");
        builder.Property(g => g.GameName).HasColumnName("GameName").HasMaxLength(100).IsRequired();
        builder.Property(g => g.IsActive).HasColumnName("IsActive");
        builder.Property(g => g.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(g => g.Description).HasColumnName("Description").HasMaxLength(500);
        builder.Property(g => g.DownloadLink).HasColumnName("DownloadLink").HasMaxLength(500);
        builder.Property(g => g.DomainId).HasColumnName("DomainId");

        builder.HasIndex(g => g.DomainId).HasDatabaseName("IX_Game_DomainId");

        // SetNull, ca in schema: stergerea unui domeniu lasa jocurile fara domeniu.
        builder.HasOne(g => g.Domain)
               .WithMany(d => d.Games)
               .HasForeignKey(g => g.DomainId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
