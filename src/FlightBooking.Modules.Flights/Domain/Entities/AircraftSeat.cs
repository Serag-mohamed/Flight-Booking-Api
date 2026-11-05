using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Domain.Entities
{
    public class AircraftSeat
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AircraftId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public CabinClass CabinClass { get; set; } = CabinClass.Economy;

        public Aircraft Aircraft { get; set; } = null!;
    }
}
