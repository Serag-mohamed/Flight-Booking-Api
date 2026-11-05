using FlightBooking.Modules.Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Modules.Bookings.Infrastructure.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasIndex(b => b.BookingReference).IsUnique();
        builder.HasIndex(b => new { b.UserId, b.CreatedAtUtc });
        builder.HasIndex(b => b.FlightId);
        builder.HasIndex(b => new { b.Status, b.ExpiresAtUtc });

        builder.Property(b => b.BookingReference).HasMaxLength(20).IsRequired();
        builder.Property(b => b.UserId).HasMaxLength(450).IsRequired();
        builder.Property(b => b.FlightNumber).HasMaxLength(20).IsRequired();
        builder.Property(b => b.OriginIata).HasMaxLength(3).IsRequired();
        builder.Property(b => b.DestinationIata).HasMaxLength(3).IsRequired();
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(b => b.TotalAmount).HasPrecision(18, 2);
        builder.Property(b => b.Currency).HasMaxLength(3).IsRequired().HasDefaultValue("USD");
        builder.Property(b => b.RowVersion).IsRowVersion();

        builder.HasMany(b => b.Passengers)
               .WithOne(p => p.Booking)
               .HasForeignKey(p => p.BookingId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Seats)
               .WithOne(s => s.Booking)
               .HasForeignKey(s => s.BookingId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BookingPassengerConfiguration : IEntityTypeConfiguration<BookingPassenger>
{
    public void Configure(EntityTypeBuilder<BookingPassenger> builder)
    {
        builder.Property(p => p.FullName).HasMaxLength(150).IsRequired();
        builder.Property(p => p.DateOfBirth).HasColumnType("date");
        builder.Property(p => p.PassengerType).HasConversion<string>().HasMaxLength(30);
    }
}

public class BookingSeatConfiguration : IEntityTypeConfiguration<BookingSeat>
{
    public void Configure(EntityTypeBuilder<BookingSeat> builder)
    {
        builder.HasIndex(s => s.BookingId);
        builder.HasIndex(s => s.PassengerId);

        builder.HasIndex(s => s.FlightSeatId)
               .IsUnique()
               .HasFilter("[IsActive] = 1");

        builder.Property(s => s.UnitPrice).HasPrecision(18, 2);
        builder.Property(s => s.SeatNumber).HasMaxLength(10).IsRequired();
        builder.Property(s => s.CabinClass).HasMaxLength(50).IsRequired();

        builder.HasOne(s => s.Passenger)
               .WithMany()
               .HasForeignKey(s => s.PassengerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
