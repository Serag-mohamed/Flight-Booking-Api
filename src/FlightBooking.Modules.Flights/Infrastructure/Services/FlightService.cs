using FlightBooking.Shared.Common;
using FlightBooking.Modules.Flights.Application.DTOs;
using FlightBooking.Modules.Flights.Application.Interfaces;
using FlightBooking.Modules.Flights.Domain.Entities;
using FlightBooking.Modules.Flights.Domain.Enums;
using FlightBooking.Modules.Flights.Domain.IRepositories;
using FlightBooking.Shared.Contracts.Bookings;
using FlightBooking.Shared.Exceptions;

namespace FlightBooking.Modules.Flights.Infrastructure.Services;

public class FlightService : IFlightService
{
    private const int MaxPageSize = 100;
    private readonly IFlightRepository _repo;
    private readonly IEnumerable<IFlightCancellationHandler> _cancellationHandlers;

    public FlightService(IFlightRepository repo, IEnumerable<IFlightCancellationHandler> cancellationHandlers)
    {
        _repo = repo;
        _cancellationHandlers = cancellationHandlers;
    }

    public async Task<PagedResult<FlightSearchResultDto>> SearchAsync(
        FlightSearchQueryParams query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        if (query.Date == default)
            throw new BusinessRuleException("A valid 'date' is required.");

        var origin = await _repo.GetAirportByIataAsync(query.Origin.Trim().ToUpperInvariant(), ct)
            ?? throw new NotFoundException($"Airport '{query.Origin}' was not found.");
        var destination = await _repo.GetAirportByIataAsync(query.Destination.Trim().ToUpperInvariant(), ct)
            ?? throw new NotFoundException($"Airport '{query.Destination}' was not found.");

        if (origin.Id == destination.Id)
            throw new BusinessRuleException("Origin and destination must be different.");

        var tz = ResolveTimeZone(origin.TimeZoneId);
        var startUtc = StartOfDayUtc(query.Date, tz);
        var endUtc = StartOfDayUtc(query.Date.AddDays(1), tz);

        var (flights, total) = await _repo.SearchAsync(
            origin.Id, destination.Id, startUtc, endUtc,
            query.CabinClass, query.Passengers, page, pageSize, ct);

        var summaries = await _repo.GetSeatSummariesAsync(
            flights.Select(f => f.Id), query.CabinClass, ct);

        var items = flights.Select(f =>
        {
            summaries.TryGetValue(f.Id, out var s);
            return new FlightSearchResultDto
            {
                Flight = ToDto(f),
                AvailableSeats = s.Available,
                LowestPrice = s.Lowest,
                Currency = s.Currency
            };
        }).ToList();

        return new PagedResult<FlightSearchResultDto>(items, page, pageSize, total);
    }

    public async Task<FlightResponseDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var flight = await _repo.GetByIdWithDetailsAsync(id, ct);
        return flight is null ? null : ToDto(flight);
    }

    public async Task<IReadOnlyList<FlightSeatResponseDto>?> GetSeatsAsync(
        Guid flightId, CabinClass? cabinClass, bool availableOnly, CancellationToken ct)
    {
        if (await _repo.GetByIdAsync(flightId, ct) is null) return null;

        var seats = await _repo.GetSeatsAsync(flightId, cabinClass, availableOnly, ct);
        return seats.Select(ToSeatDto).ToList();
    }

    public async Task<PagedResult<FlightResponseDto>> GetAllAsync(
        FlightAdminQueryParams query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        DateTimeOffset? from = query.FromDate is null ? null
            : new DateTimeOffset(query.FromDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        DateTimeOffset? to = query.ToDate is null ? null
            : new DateTimeOffset(query.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var (items, total) = await _repo.GetPagedAsync(
            query.FlightNumber?.Trim().ToUpperInvariant(), query.Status, from, to, page, pageSize, ct);

        return new PagedResult<FlightResponseDto>(items.Select(ToDto).ToList(), page, pageSize, total);
    }

    public async Task<FlightResponseDto> CreateAsync(CreateFlightRequestDto request, CancellationToken ct)
    {
        var flightNumber = request.FlightNumber.Trim().ToUpperInvariant();

        await ValidateFlightAsync(
            flightNumber, request.OriginAirportId, request.DestinationAirportId,
            request.AircraftId, request.DepartureUtc, request.ArrivalUtc,
            excludeId: null, requireFutureDeparture: true, ct);

        var flight = new Flight
        {
            FlightNumber = flightNumber,
            OriginAirportId = request.OriginAirportId,
            DestinationAirportId = request.DestinationAirportId,
            AircraftId = request.AircraftId,
            DepartureUtc = request.DepartureUtc.ToUniversalTime(),
            ArrivalUtc = request.ArrivalUtc.ToUniversalTime(),
            Status = FlightStatus.Scheduled
        };

        await _repo.AddAsync(flight, ct);
        return (await GetByIdAsync(flight.Id, ct))!;
    }

    public async Task<FlightResponseDto?> UpdateAsync(
        Guid id, UpdateFlightRequestDto request, CancellationToken ct)
    {
        var flight = await _repo.GetByIdAsync(id, ct);
        if (flight is null) return null;

        if (flight.Status != FlightStatus.Scheduled)
            throw new BusinessRuleException("Only scheduled flights can be edited.");

        var flightNumber = request.FlightNumber.Trim().ToUpperInvariant();
        var newDeparture = request.DepartureUtc.ToUniversalTime();
        var newArrival = request.ArrivalUtc.ToUniversalTime();

        if (flight.AircraftId != request.AircraftId && await _repo.HasSeatsAsync(id, ct))
            throw new BusinessRuleException(
                "Cannot change the aircraft after seats were generated for this flight.");

        var routeChanged = flight.OriginAirportId != request.OriginAirportId
                           || flight.DestinationAirportId != request.DestinationAirportId;
        if (routeChanged && await _repo.HasBookedOrHeldSeatsAsync(id, ct))
            throw new BusinessRuleException("Cannot change the route of a flight that has booked or held seats.");

        await ValidateFlightAsync(
            flightNumber, request.OriginAirportId, request.DestinationAirportId,
            request.AircraftId, newDeparture, newArrival,
            excludeId: id, requireFutureDeparture: newDeparture != flight.DepartureUtc, ct);

        flight.FlightNumber = flightNumber;
        flight.OriginAirportId = request.OriginAirportId;
        flight.DestinationAirportId = request.DestinationAirportId;
        flight.AircraftId = request.AircraftId;
        flight.DepartureUtc = newDeparture;
        flight.ArrivalUtc = newArrival;

        await _repo.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<FlightResponseDto?> ChangeStatusAsync(
        Guid id, FlightStatus status, CancellationToken ct)
    {
        var flight = await _repo.GetByIdAsync(id, ct);
        if (flight is null) return null;

        if (status == FlightStatus.Cancelled)
            return await CancelAsync(id, ct);

        if (!FlightStatusTransitions.CanTransition(flight.Status, status))
            throw new BusinessRuleException(
                $"Cannot change flight status from {flight.Status} to {status}.");

        flight.Status = status;
        await _repo.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<FlightResponseDto?> CancelAsync(Guid id, CancellationToken ct)
    {
        var flight = await _repo.GetByIdAsync(id, ct);
        if (flight is null) return null;

        if (flight.Status == FlightStatus.Cancelled)
        {
            await NotifyBookingsAsync(id, ct);
            return await GetByIdAsync(id, ct);
        }

        if (!FlightStatusTransitions.CanTransition(flight.Status, FlightStatus.Cancelled))
            throw new BusinessRuleException($"A {flight.Status} flight cannot be cancelled.");

        flight.Status = FlightStatus.Cancelled;

        var seats = await _repo.GetTrackedSeatsAsync(id, ct);
        foreach (var seat in seats.Where(s => s.Status is SeatStatus.Available or SeatStatus.Held))
            seat.Status = SeatStatus.Unavailable;

        await _repo.SaveChangesAsync(ct);

        await NotifyBookingsAsync(id, ct);
        return await GetByIdAsync(id, ct);
    }

    private async Task NotifyBookingsAsync(Guid flightId, CancellationToken ct)
    {
        var failed = 0;

        foreach (var handler in _cancellationHandlers)
        {
            var result = await handler.HandleFlightCancelledAsync(flightId, ct);
            failed += result.Failed;
        }

        if (failed > 0)
            throw new ConflictException(
                $"The flight was cancelled, but {failed} booking(s) could not be cancelled/refunded. " +
                "Call the cancel endpoint again to retry.");
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var flight = await _repo.GetByIdAsync(id, ct);
        if (flight is null) return false;

        if (await _repo.HasBookedOrHeldSeatsAsync(id, ct))
            throw new ConflictException("Cannot delete a flight that has booked or held seats.");

        await _repo.RemoveAsync(flight, ct);
        return true;
    }

    public async Task<int> GenerateSeatsAsync(
        Guid flightId, GenerateFlightSeatsRequestDto request, CancellationToken ct)
    {
        var flight = await _repo.GetByIdAsync(flightId, ct)
            ?? throw new NotFoundException($"Flight '{flightId}' was not found.");

        if (flight.Status != FlightStatus.Scheduled)
            throw new BusinessRuleException("Seats can only be generated for scheduled flights.");

        if (await _repo.HasSeatsAsync(flightId, ct))
            throw new ConflictException("Seats were already generated for this flight.");

        var aircraft = await _repo.GetAircraftWithSeatsAsync(flight.AircraftId, ct)
            ?? throw new NotFoundException("Aircraft was not found.");

        if (aircraft.AircraftSeats.Count == 0)
            throw new BusinessRuleException("The aircraft has no seats configured.");

        var prices = request.PriceByCabinClass;
        EnsurePricesValid(prices);

        var missing = aircraft.AircraftSeats
            .Select(s => s.CabinClass)
            .Distinct()
            .Where(c => !prices.ContainsKey(c))
            .ToList();

        if (missing.Count > 0)
            throw new BusinessRuleException($"Missing price for cabin class: {string.Join(", ", missing)}.");

        var currency = request.Currency.Trim().ToUpperInvariant();

        var seats = aircraft.AircraftSeats.Select(s => new FlightSeat
        {
            FlightId = flightId,
            SeatNumber = s.SeatNumber,
            CabinClass = s.CabinClass,
            Price = prices[s.CabinClass],
            Currency = currency,
            Status = SeatStatus.Available
        }).ToList();

        await _repo.AddSeatsAsync(seats, ct);
        return seats.Count;
    }

    public async Task<int> UpdateSeatPricingAsync(
        Guid flightId, UpdateFlightSeatPricingRequestDto request, CancellationToken ct)
    {
        var flight = await _repo.GetByIdAsync(flightId, ct)
            ?? throw new NotFoundException($"Flight '{flightId}' was not found.");

        if (flight.Status is FlightStatus.Departed or FlightStatus.Arrived or FlightStatus.Cancelled)
            throw new BusinessRuleException("Pricing cannot be changed for this flight.");

        var prices = request.PriceByCabinClass;
        EnsurePricesValid(prices);

        var seats = await _repo.GetTrackedSeatsAsync(flightId, ct);

        var updated = 0;
        foreach (var seat in seats.Where(s => s.Status == SeatStatus.Available))
        {
            if (prices.TryGetValue(seat.CabinClass, out var price))
            {
                seat.Price = price;
                updated++;
            }
        }

        await _repo.SaveChangesAsync(ct);
        return updated;
    }

    public async Task<FlightSeatResponseDto?> BlockSeatAsync(
        Guid flightId, Guid seatId, CancellationToken ct)
    {
        var seat = await _repo.GetSeatAsync(flightId, seatId, ct);
        if (seat is null) return null;

        if (seat.Status is SeatStatus.Held or SeatStatus.Booked)
            throw new ConflictException("Cannot block a seat that is held or booked.");

        seat.Status = SeatStatus.Unavailable;
        await _repo.SaveChangesAsync(ct);
        return ToSeatDto(seat);
    }

    private async Task ValidateFlightAsync(
        string flightNumber, Guid originId, Guid destinationId, Guid aircraftId,
        DateTimeOffset departure, DateTimeOffset arrival,
        Guid? excludeId, bool requireFutureDeparture, CancellationToken ct)
    {
        if (originId == destinationId)
            throw new BusinessRuleException("Origin and destination airports must be different.");

        if (arrival <= departure)
            throw new BusinessRuleException("Arrival time must be after departure time.");

        if (requireFutureDeparture && departure <= DateTimeOffset.UtcNow)
            throw new BusinessRuleException("Departure time must be in the future.");

        if (await _repo.GetAirportAsync(originId, ct) is null)
            throw new NotFoundException("Origin airport was not found.");
        if (await _repo.GetAirportAsync(destinationId, ct) is null)
            throw new NotFoundException("Destination airport was not found.");

        var aircraft = await _repo.GetAircraftWithSeatsAsync(aircraftId, ct)
            ?? throw new NotFoundException("Aircraft was not found.");

        if (!aircraft.IsActive)
            throw new BusinessRuleException("The selected aircraft is not active.");

        var depUtc = departure.ToUniversalTime();
        var dayStart = new DateTimeOffset(depUtc.Date, TimeSpan.Zero);

        if (await _repo.FlightNumberExistsOnDateAsync(
                flightNumber, dayStart, dayStart.AddDays(1), excludeId, ct))
            throw new ConflictException($"Flight '{flightNumber}' already exists on this date.");

        if (await _repo.AircraftHasOverlapAsync(
                aircraftId, depUtc, arrival.ToUniversalTime(), excludeId, ct))
            throw new ConflictException("The aircraft is already assigned to another flight in this period.");
    }

    private static void EnsurePricesValid(Dictionary<CabinClass, decimal> prices)
    {
        if (prices.Count == 0)
            throw new BusinessRuleException("At least one cabin class price is required.");

        if (prices.Values.Any(p => p < 0))
            throw new BusinessRuleException("Prices cannot be negative.");
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new BusinessRuleException($"The airport time zone '{timeZoneId}' is not valid on this server.");
        }
    }

    private static DateTimeOffset StartOfDayUtc(DateOnly date, TimeZoneInfo tz)
    {
        var local = date.ToDateTime(TimeOnly.MinValue);
        while (tz.IsInvalidTime(local))
            local = local.AddMinutes(30);

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, tz), TimeSpan.Zero);
    }

    private static FlightResponseDto ToDto(Flight f) => new()
    {
        Id = f.Id,
        FlightNumber = f.FlightNumber,
        OriginAirport = f.OriginAirport.ToDto(),
        DestinationAirport = f.DestinationAirport.ToDto(),
        AircraftId = f.AircraftId,
        AircraftModel = f.Aircraft.Model,
        DepartureUtc = f.DepartureUtc,
        ArrivalUtc = f.ArrivalUtc,
        Status = f.Status
    };

    private static FlightSeatResponseDto ToSeatDto(FlightSeat s) => new()
    {
        Id = s.Id,
        SeatNumber = s.SeatNumber,
        CabinClass = s.CabinClass,
        Price = s.Price,
        Currency = s.Currency,
        Status = s.Status
    };
}
