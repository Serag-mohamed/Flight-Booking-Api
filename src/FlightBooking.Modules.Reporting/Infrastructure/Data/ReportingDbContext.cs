using FlightBooking.Modules.Reporting.Domain.Entities;
using FlightBooking.Modules.Reporting.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Modules.Reporting.Infrastructure.Data;

public class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    public DbSet<GeneratedReport> GeneratedReports => Set<GeneratedReport>();

    public DbSet<BookingRow> Bookings => Set<BookingRow>();
    public DbSet<PaymentRow> Payments => Set<PaymentRow>();
    public DbSet<RefundRow> Refunds => Set<RefundRow>();
    public DbSet<FlightRow> Flights => Set<FlightRow>();
    public DbSet<FlightSeatRow> FlightSeats => Set<FlightSeatRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reporting");

        modelBuilder.Entity<GeneratedReport>(e =>
        {
            e.ToTable("GeneratedReports");
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.Scope, r.Key }).IsUnique();

            e.Property(r => r.Scope).HasMaxLength(100).IsRequired();
            e.Property(r => r.Key).HasMaxLength(100).IsRequired();
            e.Property(r => r.RequestHash).HasMaxLength(255).IsRequired();
            e.Property(r => r.ResponseBody).IsRequired();
        });

        modelBuilder.Entity<BookingRow>(e =>
        {
            e.HasNoKey();
            e.ToTable("Bookings", "bookings", t => t.ExcludeFromMigrations());
            e.Property(r => r.TotalAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PaymentRow>(e =>
        {
            e.HasNoKey();
            e.ToTable("Payments", "payments", t => t.ExcludeFromMigrations());
            e.Property(r => r.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<RefundRow>(e =>
        {
            e.HasNoKey();
            e.ToTable("Refunds", "payments", t => t.ExcludeFromMigrations());
            e.Property(r => r.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<FlightRow>(e =>
        {
            e.HasNoKey();
            e.ToTable("Flights", "flights", t => t.ExcludeFromMigrations());
        });

        modelBuilder.Entity<FlightSeatRow>(e =>
        {
            e.HasNoKey();
            e.ToTable("FlightSeats", "flights", t => t.ExcludeFromMigrations());
        });
    }
}
