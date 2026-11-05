using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Domain.Entities
{
    public class Flight
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FlightNumber { get; set; } = string.Empty;
        public Guid OriginAirportId { get; set; }
        public Guid DestinationAirportId { get; set; }
        public Guid AircraftId { get; set; }
        public DateTimeOffset DepartureUtc { get; set; }
        public DateTimeOffset ArrivalUtc { get; set; }
        public FlightStatus Status { get; set; } = FlightStatus.Scheduled;

        public Airport OriginAirport { get; set; } = null!;
        public Airport DestinationAirport { get; set; } = null!;
        public Aircraft Aircraft { get; set; } = null!;
        public ICollection<FlightSeat> FlightSeats { get; set; } = new HashSet<FlightSeat>();
    }
}
