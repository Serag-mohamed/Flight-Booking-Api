namespace FlightBooking.Modules.Bookings.Application.Interfaces;

public interface IBookingExpirationProcessor
{
    Task<int> ExpireOverdueAsync(CancellationToken ct);
}
