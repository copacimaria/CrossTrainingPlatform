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
        builder.Property(g => g.GameName).HasColumnName("GameName").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(g => g.IsActive).HasColumnName("IsActive");
        builder.Property(g => g.CreatedAt).HasColumnName("CreatedAt");
        builder.Property(g => g.Description).HasColumnName("Description").HasColumnType("nvarchar(max)");
        builder.Property(g => g.DownloadLink).HasColumnName("DownloadLink").HasColumnType("nvarchar(max)");
        builder.Property(g => g.DomainId).HasColumnName("DomainId");

        // Relatia Game -> Domain (domeniul caruia ii apartine jocul).
        // Distincta de relatia Domain.GameId, ca sa evitam ambiguitatea de FK.
        builder.HasOne(g => g.Domain)
               .WithMany(d => d.Games)
               .HasForeignKey(g => g.DomainId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
