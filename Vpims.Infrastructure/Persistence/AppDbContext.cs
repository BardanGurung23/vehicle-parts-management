using Microsoft.EntityFrameworkCore;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence.Configurations;

namespace Vpims.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<PartCategory> PartCategories => Set<PartCategory>();

    public DbSet<Part> Parts => Set<Part>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new PartCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new PartConfiguration());
    }
}