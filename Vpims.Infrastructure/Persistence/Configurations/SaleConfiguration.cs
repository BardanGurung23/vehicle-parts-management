using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales_invoices");

        builder.HasKey(s => s.SaleId);

        builder.Property(s => s.SaleId)
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

        builder.Property(s => s.Subtotal)
            .HasColumnName("subtotal")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(s => s.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasColumnType("numeric(12,2)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(s => s.TotalAmount)
            .HasColumnName("total_amount")
            .HasColumnType("numeric(12,2)")
            .IsRequired();

        builder.Property(s => s.PaymentStatus)
            .HasColumnName("payment_status")
            .HasMaxLength(30)
            .HasDefaultValue("Paid")
            .IsRequired();

        builder.Property(s => s.DueDate)
            .HasColumnName("due_date");

        builder.Property(s => s.Notes)
            .HasColumnName("notes");

        builder.Property(s => s.SaleDate)
            .HasColumnName("invoice_date")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Vehicle)
            .WithMany()
            .HasForeignKey(s => s.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.CustomerId)
            .HasDatabaseName("ix_sales_invoices_customer_id");

        builder.HasIndex(s => s.VehicleId)
            .HasDatabaseName("ix_sales_invoices_vehicle_id");

        builder.HasIndex(s => s.InvoiceNumber)
            .IsUnique();

        builder.HasIndex(s => new { s.PaymentStatus, s.DueDate })
            .HasDatabaseName("ix_sales_invoices_payment_status_due_date");
    }
}
