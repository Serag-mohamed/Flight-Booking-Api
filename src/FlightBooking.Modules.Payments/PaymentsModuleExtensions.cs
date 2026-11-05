using FlightBooking.Modules.Payments.Application.Interfaces;
using FlightBooking.Modules.Payments.Infrastructure.Data;
using FlightBooking.Modules.Payments.Infrastructure.Gateway;
using FlightBooking.Modules.Payments.Infrastructure.Services;
using FlightBooking.Shared.Contracts.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlightBooking.Modules.Payments;

public static class PaymentsModuleExtensions
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PaymentsDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "payments")));

        services.AddSingleton<IPaymentGateway, MockPaymentGateway>();

        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPaymentRefundService, PaymentRefundService>();

        return services;
    }
}
