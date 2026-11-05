using FlightBooking.Modules.Bookings.Application.Interfaces;
using FlightBooking.Modules.Bookings.Infrastructure.Data;
using FlightBooking.Modules.Bookings.Infrastructure.Services;
using FlightBooking.Shared.Contracts.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlightBooking.Modules.Bookings;

public static class BookingsModuleExtensions
{
    public static IServiceCollection AddBookingsModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BookingsDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "bookings")));

        services.AddScoped<IBookingService, BookingService>();

        services.AddScoped<BookingLifecycleService>();
        services.AddScoped<IBookingLifecycleService>(sp => sp.GetRequiredService<BookingLifecycleService>());
        services.AddScoped<IBookingExpirationProcessor>(sp => sp.GetRequiredService<BookingLifecycleService>());

        services.AddScoped<IFlightCancellationHandler, FlightCancellationHandler>();

        services.AddHostedService<BookingExpirationWorker>();

        return services;
    }
}
