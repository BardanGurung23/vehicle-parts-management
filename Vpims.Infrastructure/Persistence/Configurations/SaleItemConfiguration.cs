using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("sales_invoice_items");

        builder.HasKey(si => si.SaleItemId);

        builder.Property(si => si.SaleItemId)
            .HasColumnName("sales_invoice_item_id")
            .ValueGeneratedOnAdd();

        builder.Property(si => si.SaleId)
            .HasColumnName("sales_invoice_id")
            .IsRequired();

        builder.Property(si => si.PartId)
            .HasColumnName("part_id")
            .IsRequired();

        builder.Property(si => si.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(si => si.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(si => si.LineTotal)
            .HasColumnName("line_total")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.HasOne(si => si.Sale)
            .WithMany(s => s.Items)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(si => si.Part)
            .WithMany()
            .HasForeignKey(si => si.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(si => si.SaleId)
            .HasDatabaseName("ix_sales_invoice_items_sales_invoice_id");

        builder.HasIndex(si => si.PartId)
            .HasDatabaseName("ix_sales_invoice_items_part_id");
    }
}
