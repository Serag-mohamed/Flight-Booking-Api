using FlightBooking.Modules.Flights.Domain.Enums;

namespace FlightBooking.Modules.Flights.Domain.Entities
{
    public class FlightSeat
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FlightId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public CabinClass CabinClass { get; set; } = CabinClass.Economy;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public SeatStatus Status { get; set; } = SeatStatus.Available;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public Flight Flight { get; set; } = null!;
    }
}
