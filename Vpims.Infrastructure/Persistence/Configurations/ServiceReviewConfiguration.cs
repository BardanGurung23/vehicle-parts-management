using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class ServiceReviewConfiguration : IEntityTypeConfiguration<ServiceReview>
{
    public void Configure(EntityTypeBuilder<ServiceReview> builder)
    {
        builder.ToTable("service_reviews");

        builder.HasKey(review => review.ReviewId);

        builder.Property(review => review.ReviewId)
            .HasColumnName("review_id")
            .ValueGeneratedOnAdd();

        builder.Property(review => review.AppointmentId)
            .HasColumnName("appointment_id")
            .IsRequired();

        builder.Property(review => review.CustomerId)
            .HasColumnName("customer_id")
            .IsRequired();

        builder.Property(review => review.Rating)
            .HasColumnName("rating")
            .IsRequired();

        builder.Property(review => review.Comment)
            .HasColumnName("comment")
            .HasColumnType("text");

        builder.Property(review => review.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(review => review.AppointmentId)
            .HasDatabaseName("ix_service_reviews_appointment_id")
            .IsUnique();

        builder.HasOne(review => review.Appointment)
            .WithOne(appointment => appointment.Review)
            .HasForeignKey<ServiceReview>(review => review.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(review => review.Customer)
            .WithMany()
            .HasForeignKey(review => review.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}