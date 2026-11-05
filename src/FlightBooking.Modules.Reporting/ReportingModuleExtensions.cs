using FlightBooking.Modules.Reporting.Application.Interfaces;
using FlightBooking.Modules.Reporting.Infrastructure.Data;
using FlightBooking.Modules.Reporting.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlightBooking.Modules.Reporting;

public static class ReportingModuleExtensions
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ReportingDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "reporting")));

        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
