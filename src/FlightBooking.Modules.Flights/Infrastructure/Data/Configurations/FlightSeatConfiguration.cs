using FlightBooking.Modules.Flights.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Modules.Flights.Infrastructure.Data.Configurations;

public class FlightSeatConfiguration : IEntityTypeConfiguration<FlightSeat>
{
    public void Configure(EntityTypeBuilder<FlightSeat> builder)
    {
        builder.HasIndex(s => new { s.FlightId, s.SeatNumber }).IsUnique();
        builder.HasIndex(s => new { s.FlightId, s.Status });

        builder.Property(s => s.SeatNumber).HasMaxLength(10).IsRequired();
        builder.Property(s => s.CabinClass).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.Currency).HasMaxLength(3).IsRequired();
        builder.Property(s => s.Price).HasPrecision(18, 2);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(30);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}
