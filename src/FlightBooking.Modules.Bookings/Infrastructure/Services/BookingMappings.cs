using FlightBooking.Modules.Bookings.Application.DTOs;
using FlightBooking.Modules.Bookings.Domain.Entities;

namespace FlightBooking.Modules.Bookings.Infrastructure.Services;

internal static class BookingMappings
{
    public static BookingResponseDto ToDto(this Booking b)
    {
        var seatByPassenger = b.Seats.ToDictionary(s => s.PassengerId);

        return new BookingResponseDto
        {
            Id = b.Id,
            BookingReference = b.BookingReference,
            FlightId = b.FlightId,
            FlightNumber = b.FlightNumber,
            OriginIata = b.OriginIata,
            DestinationIata = b.DestinationIata,
            DepartureUtc = b.DepartureUtc,
            Status = b.Status,
            TotalAmount = b.TotalAmount,
            Currency = b.Currency,
            ExpiresAtUtc = b.ExpiresAtUtc,
            CreatedAtUtc = b.CreatedAtUtc,
            Passengers = b.Passengers
                .Select(p =>
                {
                    var seat = seatByPassenger[p.Id];
                    return new BookingPassengerResponseDto
                    {
                        PassengerId = p.Id,
                        FullName = p.FullName,
                        DateOfBirth = p.DateOfBirth,
                        PassengerType = p.PassengerType,
                        FlightSeatId = seat.FlightSeatId,
                        SeatNumber = seat.SeatNumber,
                        CabinClass = seat.CabinClass,
                        UnitPrice = seat.UnitPrice
                    };
                })
                .OrderBy(p => p.SeatNumber.Length).ThenBy(p => p.SeatNumber)
                .ToList()
        };
    }
}
