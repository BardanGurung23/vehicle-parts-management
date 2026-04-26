using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.ToTable("parts");

        builder.HasKey(p => p.PartId);

        builder.Property(p => p.PartId)
            .HasColumnName("part_id")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.PartCategoryId)
            .HasColumnName("part_category_id");

        builder.Property(p => p.PartNumber)
            .HasColumnName("part_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.PartName)
            .HasColumnName("part_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description");

        builder.Property(p => p.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(p => p.CostPrice)
            .HasColumnName("cost_price")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(p => p.StockQuantity)
            .HasColumnName("stock_quantity")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(p => p.ReorderLevel)
            .HasColumnName("reorder_level")
            .HasDefaultValue(10)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Parts)
            .HasForeignKey(p => p.PartCategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.PartNumber).IsUnique();
        builder.HasIndex(p => p.PartName).HasDatabaseName("ix_parts_part_name");
        builder.HasIndex(p => p.StockQuantity).HasDatabaseName("ix_parts_stock_quantity");
    }
}
