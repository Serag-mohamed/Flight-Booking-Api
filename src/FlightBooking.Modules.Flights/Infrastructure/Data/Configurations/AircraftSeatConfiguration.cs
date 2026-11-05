using FlightBooking.Modules.Flights.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Modules.Flights.Infrastructure.Data.Configurations;

public class AircraftSeatConfiguration : IEntityTypeConfiguration<AircraftSeat>
{
    public void Configure(EntityTypeBuilder<AircraftSeat> builder)
    {
        builder.HasIndex(s => new { s.AircraftId, s.SeatNumber }).IsUnique();

        builder.Property(s => s.SeatNumber).HasMaxLength(10).IsRequired();
        builder.Property(s => s.CabinClass).HasConversion<string>().HasMaxLength(50);
    }
}
