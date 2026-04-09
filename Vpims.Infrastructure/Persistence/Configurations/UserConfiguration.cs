using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vpims.Domain.Entities;

namespace Vpims.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.UserId);

        builder.Property(user => user.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedOnAdd();

        builder.Property(user => user.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(user => user.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(user => user.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasOne(user => user.Role)
            .WithMany(role => role.Users)
            .HasForeignKey(user => user.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(user => user.Customer)
            .WithOne(customer => customer.User)
            .HasForeignKey<Customer>(customer => customer.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.HasIndex(user => user.PhoneNumber)
            .IsUnique();

        builder.HasIndex(user => user.FullName)
            .HasDatabaseName("ix_users_full_name");

        builder.HasIndex(user => user.PhoneNumber)
            .HasDatabaseName("ix_users_phone_number");
    }
}