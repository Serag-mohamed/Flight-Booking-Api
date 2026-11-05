using FlightBooking.Modules.Flights.Application.Interfaces;
using FlightBooking.Modules.Flights.Domain.IRepositories;
using FlightBooking.Modules.Flights.Infrastructure.Data;
using FlightBooking.Modules.Flights.Infrastructure.Repositories;
using FlightBooking.Modules.Flights.Infrastructure.Services;
using FlightBooking.Shared.Contracts.Flights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlightBooking.Modules.Flights;

public static class FlightsModuleExtensions
{
    public static IServiceCollection AddFlightsModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FlightsDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "flights")));

        services.AddScoped<IAirportRepository, AirportRepository>();
        services.AddScoped<IAircraftRepository, AircraftRepository>();
        services.AddScoped<IFlightRepository, FlightRepository>();

        services.AddScoped<IAirportService, AirportService>();
        services.AddScoped<IAircraftService, AircraftService>();
        services.AddScoped<IFlightService, FlightService>();

        services.AddScoped<IFlightSeatReservationService, FlightSeatReservationService>();

        return services;
    }
}
