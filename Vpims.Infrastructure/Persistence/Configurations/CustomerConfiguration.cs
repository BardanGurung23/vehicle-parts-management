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
            .IsRequired();

        builder.Property(customer => customer.Address)
            .HasColumnName("address");

        builder.Property(customer => customer.RegisteredAt)
            .HasColumnName("registered_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(customer => customer.UserId)
            .IsUnique();
    }
}