using FlightBooking.Modules.Flights.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Modules.Flights.Infrastructure.Data.Configurations;

public class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.Property(f => f.FlightNumber).HasMaxLength(20).IsRequired();
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(f => new { f.OriginAirportId, f.DestinationAirportId, f.DepartureUtc });
        builder.HasIndex(f => new { f.AircraftId, f.DepartureUtc });

        builder.HasOne(f => f.OriginAirport)
               .WithMany(a => a.DepartureFlights)
               .HasForeignKey(f => f.OriginAirportId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.DestinationAirport)
               .WithMany(a => a.ArrivalFlights)
               .HasForeignKey(f => f.DestinationAirportId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.FlightSeats)
               .WithOne(s => s.Flight)
               .HasForeignKey(s => s.FlightId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
