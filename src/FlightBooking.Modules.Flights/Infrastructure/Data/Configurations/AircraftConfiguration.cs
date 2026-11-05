using FlightBooking.Modules.Flights.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlightBooking.Modules.Flights.Infrastructure.Data.Configurations;

public class AircraftConfiguration : IEntityTypeConfiguration<Aircraft>
{
    public void Configure(EntityTypeBuilder<Aircraft> builder)
    {
        builder.HasIndex(a => a.RegistrationNumber).IsUnique();
        builder.Property(a => a.RegistrationNumber).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Model).HasMaxLength(100).IsRequired();

        builder.HasMany(a => a.AircraftSeats)
               .WithOne(s => s.Aircraft)
               .HasForeignKey(s => s.AircraftId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Flights)
               .WithOne(f => f.Aircraft)
               .HasForeignKey(f => f.AircraftId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
