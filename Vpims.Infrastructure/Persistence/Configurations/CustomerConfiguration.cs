using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(customer => customer.CustomerId);

        builder.Property(customer => customer.CustomerId)
            .HasColumnName("customer_id")
            .ValueGeneratedOnAdd();

        builder.Property(customer => customer.UserId)
            .HasColumnName("user_id")
            .IsRequired(false);

        builder.Property(customer => customer.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(customer => customer.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasColumnName("email")
            .HasMaxLength(150);

        builder.Property(customer => customer.Address)
            .HasColumnName("address");

        builder.Property(customer => customer.RegisteredAt)
            .HasColumnName("registered_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(customer => customer.UserId)
            .IsUnique();

        builder.HasIndex(customer => customer.PhoneNumber)
            .IsUnique();

        builder.HasIndex(customer => customer.Email)
            .IsUnique();

        builder.HasOne(customer => customer.User)
            .WithOne(user => user.Customer)
            .HasForeignKey<Customer>(customer => customer.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(customer => customer.Vehicles)
            .WithOne(vehicle => vehicle.Customer)
            .HasForeignKey(vehicle => vehicle.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}