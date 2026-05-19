using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_vehicles_mileage_non_negative", "mileage IS NULL OR mileage >= 0");
            tableBuilder.HasCheckConstraint("ck_vehicles_manufacture_year_range", "manufacture_year IS NULL OR (manufacture_year >= 1950 AND manufacture_year <= 2100)");
        });

        builder.HasKey(vehicle => vehicle.VehicleId);

        builder.Property(vehicle => vehicle.VehicleId)
            .HasColumnName("vehicle_id")
            .ValueGeneratedOnAdd();

        builder.Property(vehicle => vehicle.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(vehicle => vehicle.VehicleNumber)
            .HasColumnName("vehicle_number")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(vehicle => vehicle.Model)
            .HasColumnName("model")
            .HasMaxLength(80);

        builder.Property(vehicle => vehicle.Mileage)
            .HasColumnName("mileage");

        builder.Property(vehicle => vehicle.ManufactureYear)
            .HasColumnName("manufacture_year");

        builder.Property(vehicle => vehicle.LastServiceDate)
            .HasColumnName("last_service_date");

        builder.Property(vehicle => vehicle.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(vehicle => vehicle.CustomerId)
            .HasDatabaseName("ix_vehicles_customer_id");

        builder.HasIndex(vehicle => vehicle.VehicleNumber)
            .IsUnique()
            .HasDatabaseName("ix_vehicles_vehicle_number");
    }
}