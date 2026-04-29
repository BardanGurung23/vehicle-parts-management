using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class PartRequestConfiguration : IEntityTypeConfiguration<PartRequest>
{
    public void Configure(EntityTypeBuilder<PartRequest> builder)
    {
        builder.ToTable("part_requests");

        builder.HasKey(request => request.RequestId);

        builder.Property(request => request.RequestId)
            .HasColumnName("part_request_id")
            .ValueGeneratedOnAdd();

        builder.Property(request => request.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(request => request.VehicleId)
            .HasColumnName("vehicle_id");

        builder.Property(request => request.RequestedPartName)
            .HasColumnName("requested_part_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(request => request.RequestDetails)
            .HasColumnName("request_details")
            .HasColumnType("text");

        builder.Property(request => request.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasDefaultValue("Pending")
            .IsRequired();

        builder.Property(request => request.RequestedAt)
            .HasColumnName("requested_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(request => request.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.HasIndex(request => request.CustomerId)
            .HasDatabaseName("ix_part_requests_customer_id");

        builder.HasIndex(request => new { request.Status, request.RequestedAt })
            .HasDatabaseName("ix_part_requests_status_requested_at");

        builder.HasOne(request => request.Customer)
            .WithMany()
            .HasForeignKey(request => request.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(request => request.Vehicle)
            .WithMany()
            .HasForeignKey(request => request.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}