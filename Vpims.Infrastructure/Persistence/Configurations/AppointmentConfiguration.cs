using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");

        builder.HasKey(appointment => appointment.AppointmentId);

        builder.Property(appointment => appointment.AppointmentId)
            .HasColumnName("appointment_id")
            .ValueGeneratedOnAdd();

        builder.Property(appointment => appointment.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(appointment => appointment.VehicleId)
            .HasColumnName("vehicle_id")
            .IsRequired();

        builder.Property(appointment => appointment.AppointmentDate)
            .HasColumnName("appointment_date")
            .IsRequired();

        builder.Property(appointment => appointment.ServiceType)
            .HasColumnName("service_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(appointment => appointment.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasDefaultValue("Pending")
            .IsRequired();

        builder.Property(appointment => appointment.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(appointment => appointment.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(appointment => appointment.CustomerId)
            .HasDatabaseName("ix_appointments_customer_id");

        builder.HasIndex(appointment => appointment.VehicleId)
            .HasDatabaseName("ix_appointments_vehicle_id");

        builder.HasOne(appointment => appointment.Customer)
            .WithMany()
            .HasForeignKey(appointment => appointment.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(appointment => appointment.Vehicle)
            .WithMany()
            .HasForeignKey(appointment => appointment.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}