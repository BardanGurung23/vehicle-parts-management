using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class PartCategoryConfiguration : IEntityTypeConfiguration<PartCategory>
{
    public void Configure(EntityTypeBuilder<PartCategory> builder)
    {
        builder.ToTable("part_categories");

        builder.HasKey(c => c.PartCategoryId);

        builder.Property(c => c.PartCategoryId)
            .HasColumnName("part_category_id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.CategoryName)
            .HasColumnName("category_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description");

        builder.HasIndex(c => c.CategoryName).IsUnique();
    }
}
