using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.ToTable("purchase_invoices");

        builder.HasKey(invoice => invoice.PurchaseInvoiceId);

        builder.Property(invoice => invoice.PurchaseInvoiceId)
            .HasColumnName("purchase_invoice_id")
            .ValueGeneratedOnAdd();

        builder.Property(invoice => invoice.VendorId)
            .HasColumnName("vendor_id")
            .IsRequired();

        builder.Property(invoice => invoice.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(invoice => invoice.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(invoice => invoice.InvoiceDate)
            .HasColumnName("invoice_date")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(invoice => invoice.TotalAmount)
            .HasColumnName("total_amount")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(invoice => invoice.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasDefaultValue("Completed")
            .IsRequired();

        builder.HasOne(invoice => invoice.Vendor)
            .WithMany()
            .HasForeignKey(invoice => invoice.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(invoice => invoice.CreatedByUser)
            .WithMany()
            .HasForeignKey(invoice => invoice.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(invoice => invoice.VendorId)
            .HasDatabaseName("ix_purchase_invoices_vendor_id");

        builder.HasIndex(invoice => invoice.InvoiceNumber)
            .IsUnique();
    }
}