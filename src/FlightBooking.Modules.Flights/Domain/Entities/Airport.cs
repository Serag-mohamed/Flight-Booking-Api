namespace FlightBooking.Modules.Flights.Domain.Entities
{
    public class Airport
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string IataCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string TimeZoneId { get; set; } = string.Empty;

        public ICollection<Flight> DepartureFlights { get; set; } = new HashSet<Flight>();
        public ICollection<Flight> ArrivalFlights { get; set; } = new HashSet<Flight>();
    }
}
