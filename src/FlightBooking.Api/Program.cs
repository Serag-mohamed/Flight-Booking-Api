using System.Text.Json.Serialization;
using FlightBooking.Api.ExceptionHandling;
using FlightBooking.Api.Extensions;
using FlightBooking.Modules.Bookings;
using FlightBooking.Modules.Flights;
using FlightBooking.Modules.Identity;
using FlightBooking.Modules.Payments;
using FlightBooking.Modules.Reporting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(IdentityModuleExtensions).Assembly)
    .AddApplicationPart(typeof(FlightsModuleExtensions).Assembly)
    .AddApplicationPart(typeof(BookingsModuleExtensions).Assembly)
    .AddApplicationPart(typeof(PaymentsModuleExtensions).Assembly)
    .AddApplicationPart(typeof(ReportingModuleExtensions).Assembly)
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddIdentityModule(connectionString)
    .AddFlightsModule(connectionString)
    .AddBookingsModule(connectionString)
    .AddPaymentsModule(connectionString)
    .AddReportingModule(connectionString);

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
