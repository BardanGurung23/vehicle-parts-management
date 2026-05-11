using Microsoft.EntityFrameworkCore;
using Vpims.Domain.Models;

namespace Vpims.Infrastructure.Data;

public class VpimsDbContext(DbContextOptions<VpimsDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<VehiclePart> VehicleParts => Set<VehiclePart>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(customer => customer.FullName).IsRequired();
            entity.Property(customer => customer.Email).IsRequired();
        });

        modelBuilder.Entity<VehiclePart>(entity =>
        {
            entity.HasIndex(part => part.PartNumber).IsUnique();
            entity.Property(part => part.PartNumber).IsRequired();
            entity.Property(part => part.Name).IsRequired();
            entity.Property(part => part.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasIndex(sale => sale.InvoiceNumber).IsUnique();
            entity.Property(sale => sale.InvoiceNumber).IsRequired();
            entity.Property(sale => sale.Subtotal).HasPrecision(18, 2);
            entity.Property(sale => sale.DiscountAmount).HasPrecision(18, 2);
            entity.Property(sale => sale.FinalTotal).HasPrecision(18, 2);

            entity.HasOne(sale => sale.Customer)
                .WithMany(customer => customer.Sales)
                .HasForeignKey(sale => sale.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.Property(item => item.LineTotal).HasPrecision(18, 2);

            entity.HasOne(item => item.Sale)
                .WithMany(sale => sale.Items)
                .HasForeignKey(item => item.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.VehiclePart)
                .WithMany(part => part.SaleItems)
                .HasForeignKey(item => item.VehiclePartId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
