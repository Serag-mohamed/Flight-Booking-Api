using FlightBooking.Modules.Flights.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Modules.Flights.Infrastructure.Data;

public class FlightsDbContext : DbContext
{
    public FlightsDbContext(DbContextOptions<FlightsDbContext> options) : base(options) { }

    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Aircraft> Aircrafts => Set<Aircraft>();
    public DbSet<AircraftSeat> AircraftSeats => Set<AircraftSeat>();
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<FlightSeat> FlightSeats => Set<FlightSeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("flights");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlightsDbContext).Assembly);
    }
}
