using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class PredictiveAlertConfiguration : IEntityTypeConfiguration<PredictiveAlert>
{
    public void Configure(EntityTypeBuilder<PredictiveAlert> builder)
    {
        builder.ToTable("predictive_alerts");

        builder.HasKey(alert => alert.PredictiveAlertId);

        builder.Property(alert => alert.PredictiveAlertId)
            .HasColumnName("predictive_alert_id")
            .ValueGeneratedOnAdd();

        builder.Property(alert => alert.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(alert => alert.VehicleId)
            .HasColumnName("vehicle_id")
            .IsRequired();

        builder.Property(alert => alert.PartId)
            .HasColumnName("part_id");

        builder.Property(alert => alert.AlertMessage)
            .HasColumnName("alert_message")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(alert => alert.RiskLevel)
            .HasColumnName("risk_level")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(alert => alert.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasDefaultValue("Active")
            .IsRequired();

        builder.Property(alert => alert.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasOne(alert => alert.Customer)
            .WithMany()
            .HasForeignKey(alert => alert.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(alert => alert.Vehicle)
            .WithMany()
            .HasForeignKey(alert => alert.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(alert => alert.Part)
            .WithMany()
            .HasForeignKey(alert => alert.PartId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(alert => alert.CustomerId)
            .HasDatabaseName("ix_predictive_alerts_customer_id");

        builder.HasIndex(alert => alert.VehicleId)
            .HasDatabaseName("ix_predictive_alerts_vehicle_id");
    }
}