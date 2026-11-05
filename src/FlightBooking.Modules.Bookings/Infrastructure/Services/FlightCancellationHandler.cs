using FlightBooking.Modules.Bookings.Domain.Enums;
using FlightBooking.Modules.Bookings.Infrastructure.Data;
using FlightBooking.Shared.Contracts.Bookings;
using FlightBooking.Shared.Contracts.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlightBooking.Modules.Bookings.Infrastructure.Services;

public class FlightCancellationHandler : IFlightCancellationHandler
{
    private readonly BookingsDbContext _db;
    private readonly IPaymentRefundService _refunds;
    private readonly ILogger<FlightCancellationHandler> _logger;

    public FlightCancellationHandler(
        BookingsDbContext db, IPaymentRefundService refunds, ILogger<FlightCancellationHandler> logger)
    {
        _db = db;
        _refunds = refunds;
        _logger = logger;
    }

    public async Task<FlightCancellationResult> HandleFlightCancelledAsync(Guid flightId, CancellationToken ct)
    {
        var ids = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.FlightId == flightId
                        && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .Select(b => b.Id)
            .ToListAsync(ct);

        int cancelled = 0, refunded = 0, failed = 0;

        foreach (var id in ids)
        {
            _db.ChangeTracker.Clear();

            try
            {
                var booking = await _db.Bookings.Include(b => b.Seats).FirstOrDefaultAsync(b => b.Id == id, ct);
                if (booking is null) continue;

                if (booking.Status == BookingStatus.Confirmed)
                {
                    if (await _refunds.RefundBookingAsync(booking.Id, "Flight cancelled", ct))
                        refunded++;
                }
                else if (booking.Status != BookingStatus.Pending)
                {
                    continue;
                }

                booking.Status = BookingStatus.Cancelled;
                foreach (var seat in booking.Seats) seat.IsActive = false;

                await _db.SaveChangesAsync(ct);
                cancelled++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                _logger.LogError(ex, "Failed to cancel booking {BookingId} for cancelled flight {FlightId}.", id, flightId);
            }
        }

        return new FlightCancellationResult(cancelled, refunded, failed);
    }
}
