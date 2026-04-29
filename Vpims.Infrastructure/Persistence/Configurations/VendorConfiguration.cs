using Microsoft.EntityFrameworkCore;
using Vpims.Domain.Entities;
using Vpims.Infrastructure.Persistence;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("vendors");

        builder.HasKey(v => v.VendorId);

        builder.Property(v => v.VendorId)
            .HasColumnName("vendor_id")
            .ValueGeneratedOnAdd();

        builder.Property(v => v.VendorName)
            .HasColumnName("vendor_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(v => v.ContactPerson)
            .HasColumnName("contact_person")
            .HasMaxLength(150);

        builder.Property(v => v.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(20);

        builder.Property(v => v.Email)
            .HasColumnName("email")
            .HasMaxLength(150);

        builder.Property(v => v.Address)
            .HasColumnName("address");

        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(v => v.VendorName)
            .HasDatabaseName("ix_vendors_vendor_name");
    }
}
