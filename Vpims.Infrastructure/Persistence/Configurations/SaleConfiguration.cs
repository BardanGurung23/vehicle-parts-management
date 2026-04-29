using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales");

        builder.HasKey(s => s.SaleId);

        builder.Property(s => s.SaleId)
            .HasColumnName("sale_id")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(s => s.VehicleId)
            .HasColumnName("vehicle_id");

        builder.Property(s => s.TotalAmount)
            .HasColumnName("total_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(s => s.Notes)
            .HasColumnName("notes");

        builder.Property(s => s.SaleDate)
            .HasColumnName("sale_date")
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

        builder.HasIndex(s => s.CustomerId)
            .HasDatabaseName("ix_sales_customer_id");

        builder.HasIndex(s => s.SaleDate)
            .HasDatabaseName("ix_sales_sale_date");
    }
}
