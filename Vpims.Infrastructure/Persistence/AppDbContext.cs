using Microsoft.EntityFrameworkCore;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence.Configurations;

namespace Vpims.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<PartCategory> PartCategories => Set<PartCategory>();

    public DbSet<Part> Parts => Set<Part>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<ServiceReview> ServiceReviews => Set<ServiceReview>();

    public DbSet<PartRequest> PartRequests => Set<PartRequest>();

    public DbSet<Vendor> Vendors => Set<Vendor>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();

    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();

    public DbSet<PredictiveAlert> PredictiveAlerts => Set<PredictiveAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new VehicleConfiguration());
        modelBuilder.ApplyConfiguration(new PartCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new PartConfiguration());
        modelBuilder.ApplyConfiguration(new AppointmentConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceReviewConfiguration());
        modelBuilder.ApplyConfiguration(new PartRequestConfiguration());
        modelBuilder.ApplyConfiguration(new VendorConfiguration());
        modelBuilder.ApplyConfiguration(new SaleConfiguration());
        modelBuilder.ApplyConfiguration(new SaleItemConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseInvoiceItemConfiguration());
        modelBuilder.ApplyConfiguration(new PredictiveAlertConfiguration());
    }
}