using CrossTraining.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossTraining.Infrastructure.Persistence.Configurations;

public class DomainConfiguration : IEntityTypeConfiguration<TrainingDomain>
{
    public void Configure(EntityTypeBuilder<TrainingDomain> builder)
    {
        builder.ToTable("Domain");
        builder.HasKey(d => d.DomainId);

        builder.Property(d => d.DomainId).HasColumnName("DomainId");
        builder.Property(d => d.DomainName).HasColumnName("DomainName").HasMaxLength(150).IsRequired();
        builder.Property(d => d.IsActive).HasColumnName("IsActive");
    }
}
