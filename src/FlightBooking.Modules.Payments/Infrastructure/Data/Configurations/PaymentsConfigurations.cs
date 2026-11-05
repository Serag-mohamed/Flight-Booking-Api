using FlightBooking.Modules.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Modules.Payments.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasIndex(p => new { p.BookingId, p.Status });

        builder.HasIndex(p => new { p.UserId, p.IdempotencyKey }).IsUnique();

        builder.HasIndex(p => p.BookingId)
               .IsUnique()
               .HasFilter("[Status] = 'Succeeded'")
               .HasDatabaseName("IX_Payments_BookingId_Succeeded");

        builder.Property(p => p.UserId).HasMaxLength(450).IsRequired();
        builder.Property(p => p.ProviderReference).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired().HasDefaultValue("USD");
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.FailureReason).HasMaxLength(250);
        builder.Property(p => p.IdempotencyKey).HasMaxLength(100).IsRequired();

        builder.HasMany(p => p.Refunds)
               .WithOne(r => r.Payment)
               .HasForeignKey(r => r.PaymentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.HasIndex(r => r.PaymentId);

        builder.Property(r => r.Amount).HasPrecision(18, 2);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Reason).HasMaxLength(250).IsRequired();
    }
}

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.HasIndex(r => new { r.Scope, r.Key }).IsUnique();

        builder.Property(r => r.Scope).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Key).HasMaxLength(100).IsRequired();
        builder.Property(r => r.RequestHash).HasMaxLength(255).IsRequired();
        builder.Property(r => r.ResponseBody).IsRequired();
    }
}
