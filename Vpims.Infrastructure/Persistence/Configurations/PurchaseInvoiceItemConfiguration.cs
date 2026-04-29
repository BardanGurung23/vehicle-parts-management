using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class PurchaseInvoiceItemConfiguration : IEntityTypeConfiguration<PurchaseInvoiceItem>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceItem> builder)
    {
        builder.ToTable("purchase_invoice_items");

        builder.HasKey(item => item.PurchaseInvoiceItemId);

        builder.Property(item => item.PurchaseInvoiceItemId)
            .HasColumnName("purchase_invoice_item_id")
            .ValueGeneratedOnAdd();

        builder.Property(item => item.PurchaseInvoiceId)
            .HasColumnName("purchase_invoice_id")
            .IsRequired();

        builder.Property(item => item.PartId)
            .HasColumnName("part_id")
            .IsRequired();

        builder.Property(item => item.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(item => item.UnitCost)
            .HasColumnName("unit_cost")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(item => item.LineTotal)
            .HasColumnName("line_total")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.HasOne(item => item.PurchaseInvoice)
            .WithMany(invoice => invoice.Items)
            .HasForeignKey(item => item.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Part)
            .WithMany()
            .HasForeignKey(item => item.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.PartId)
            .HasDatabaseName("ix_purchase_invoice_items_part_id");
    }
}