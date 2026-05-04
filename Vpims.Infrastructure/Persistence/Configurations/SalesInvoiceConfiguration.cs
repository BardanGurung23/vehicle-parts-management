using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("sales_invoices");

        builder.HasKey(s => s.SalesInvoiceId);

        builder.Property(s => s.SalesInvoiceId)
            .HasColumnName("sales_invoice_id")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(s => s.VehicleId)
            .HasColumnName("vehicle_id");

        builder.Property(s => s.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(s => s.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.InvoiceDate)
            .HasColumnName("invoice_date")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(s => s.Subtotal)
            .HasColumnName("subtotal")
            .HasColumnType("numeric(12,2)")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasColumnType("numeric(12,2)")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.TotalAmount)
            .HasColumnName("total_amount")
            .HasColumnType("numeric(12,2)")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.PaymentStatus)
            .HasColumnName("payment_status")
            .HasMaxLength(30)
            .HasDefaultValue("Pending")
            .IsRequired();

        builder.Property(s => s.DueDate)
            .HasColumnName("due_date");

        builder.HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.InvoiceNumber).IsUnique();
        builder.HasIndex(s => s.CustomerId).HasDatabaseName("ix_sales_invoices_customer_id");
        builder.HasIndex(s => new { s.PaymentStatus, s.DueDate })
            .HasDatabaseName("ix_sales_invoices_payment_status_due_date");
    }
}
