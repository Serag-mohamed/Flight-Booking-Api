namespace FlightBooking.Modules.Flights.Domain.Entities
{
    public class Aircraft
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RegistrationNumber { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public ICollection<AircraftSeat> AircraftSeats { get; set; } = new HashSet<AircraftSeat>();
        public ICollection<Flight> Flights { get; set; } = new HashSet<Flight>();
    }
}
