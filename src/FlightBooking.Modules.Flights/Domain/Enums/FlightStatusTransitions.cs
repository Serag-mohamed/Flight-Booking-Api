namespace FlightBooking.Modules.Flights.Domain.Enums
{
    public static class FlightStatusTransitions
    {
        private static readonly Dictionary<FlightStatus, FlightStatus[]> Allowed = new()
        {
            [FlightStatus.Scheduled] = new[] { FlightStatus.Boarding, FlightStatus.Cancelled },
            [FlightStatus.Boarding]  = new[] { FlightStatus.Departed, FlightStatus.Cancelled },
            [FlightStatus.Departed]  = new[] { FlightStatus.Arrived },
            [FlightStatus.Arrived]   = Array.Empty<FlightStatus>(),
            [FlightStatus.Cancelled] = Array.Empty<FlightStatus>()
        };

        public static bool CanTransition(FlightStatus from, FlightStatus to)
            => Allowed[from].Contains(to);
    }
}
