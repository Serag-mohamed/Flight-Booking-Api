using FlightBooking.Shared.Common;
using FlightBooking.Modules.Flights.Application.DTOs;
using FlightBooking.Modules.Flights.Application.Interfaces;
using FlightBooking.Modules.Flights.Domain.Entities;
using FlightBooking.Modules.Flights.Domain.IRepositories;
using FlightBooking.Shared.Exceptions;

namespace FlightBooking.Modules.Flights.Infrastructure.Services;

public class AircraftService : IAircraftService
{
    private const int MaxPageSize = 100;
    private const int MaxSeatsPerAircraft = 900;
    private readonly IAircraftRepository _repo;

    public AircraftService(IAircraftRepository repo) => _repo = repo;

    public async Task<PagedResult<AircraftResponseDto>> GetAllAsync(
        AircraftQueryParams query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var (items, total) = await _repo.GetPagedAsync(
            query.Search?.Trim(), query.IsActive, page, pageSize, ct);

        var counts = await _repo.GetSeatsCountsAsync(items.Select(a => a.Id), ct);

        return new PagedResult<AircraftResponseDto>(
            Items: items.Select(a => ToDto(a, counts.GetValueOrDefault(a.Id))).ToList(),
            Page: page,
            PageSize: pageSize,
            TotalCount: total);
    }

    public async Task<AircraftResponseDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var aircraft = await _repo.GetByIdAsync(id, ct);
        if (aircraft is null) return null;

        return ToDto(aircraft, await _repo.CountSeatsAsync(id, ct));
    }

    public async Task<AircraftResponseDto> AddAsync(AddAircraftRequestDto request, CancellationToken ct)
    {
        var registration = NormalizeRegistration(request.RegistrationNumber);

        if (await _repo.RegistrationExistsAsync(registration, null, ct))
            throw new ConflictException($"Aircraft with registration '{registration}' already exists.");

        var entity = new Aircraft
        {
            RegistrationNumber = registration,
            Model = request.Model.Trim()
        };

        await _repo.AddAsync(entity, ct);
        return ToDto(entity, 0);
    }

    public async Task<AircraftResponseDto?> UpdateAsync(
        Guid id, UpdateAircraftRequestDto request, CancellationToken ct)
    {
        var aircraft = await _repo.GetByIdAsync(id, ct);
        if (aircraft is null) return null;

        var registration = NormalizeRegistration(request.RegistrationNumber);

        if (await _repo.RegistrationExistsAsync(registration, id, ct))
            throw new ConflictException($"Aircraft with registration '{registration}' already exists.");

        aircraft.RegistrationNumber = registration;
        aircraft.Model = request.Model.Trim();

        await _repo.SaveChangesAsync(ct);
        return ToDto(aircraft, await _repo.CountSeatsAsync(id, ct));
    }

    public async Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var aircraft = await _repo.GetByIdAsync(id, ct);
        if (aircraft is null) return false;

        aircraft.IsActive = isActive;
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<AircraftSeatResponseDto>?> GetSeatsAsync(
        Guid aircraftId, CancellationToken ct)
    {
        if (await _repo.GetByIdAsync(aircraftId, ct) is null) return null;

        var seats = await _repo.GetSeatsAsync(aircraftId, ct);
        return seats.Select(ToSeatDto).ToList();
    }

    public async Task<AircraftSeatResponseDto> AddSeatAsync(
        Guid aircraftId, AddAircraftSeatRequestDto request, CancellationToken ct)
    {
        await EnsureAircraftExistsAsync(aircraftId, ct);

        var seatNumber = request.SeatNumber.Trim().ToUpperInvariant();
        var existing = await _repo.GetSeatNumbersAsync(aircraftId, ct);

        if (existing.Contains(seatNumber))
            throw new ConflictException($"Seat '{seatNumber}' already exists on this aircraft.");

        if (existing.Count >= MaxSeatsPerAircraft)
            throw new BusinessRuleException($"An aircraft cannot have more than {MaxSeatsPerAircraft} seats.");

        var seat = new AircraftSeat
        {
            AircraftId = aircraftId,
            SeatNumber = seatNumber,
            CabinClass = request.CabinClass
        };

        await _repo.AddSeatsAsync(new[] { seat }, ct);
        return ToSeatDto(seat);
    }

    public async Task<IReadOnlyList<AircraftSeatResponseDto>> BulkAddSeatsAsync(
        Guid aircraftId, BulkAddAircraftSeatsRequestDto request, CancellationToken ct)
    {
        await EnsureAircraftExistsAsync(aircraftId, ct);

        if (request.ToRow < request.FromRow)
            throw new BusinessRuleException("'ToRow' must be greater than or equal to 'FromRow'.");

        var letters = request.Letters
            .Select(l => l.Trim().ToUpperInvariant())
            .Where(l => l.Length == 1 && char.IsLetter(l[0]))
            .Distinct()
            .ToList();

        if (letters.Count == 0)
            throw new BusinessRuleException("At least one valid seat letter is required.");

        var existing = await _repo.GetSeatNumbersAsync(aircraftId, ct);
        var newSeats = new List<AircraftSeat>();

        for (var row = request.FromRow; row <= request.ToRow; row++)
        {
            foreach (var letter in letters)
            {
                var number = $"{row}{letter}";
                if (existing.Contains(number))
                    throw new ConflictException($"Seat '{number}' already exists on this aircraft.");

                newSeats.Add(new AircraftSeat
                {
                    AircraftId = aircraftId,
                    SeatNumber = number,
                    CabinClass = request.CabinClass
                });
            }
        }

        if (existing.Count + newSeats.Count > MaxSeatsPerAircraft)
            throw new BusinessRuleException($"An aircraft cannot have more than {MaxSeatsPerAircraft} seats.");

        await _repo.AddSeatsAsync(newSeats, ct);
        return newSeats.Select(ToSeatDto).ToList();
    }

    public async Task<AircraftSeatResponseDto?> UpdateSeatAsync(
        Guid aircraftId, Guid seatId, UpdateAircraftSeatRequestDto request, CancellationToken ct)
    {
        var seat = await _repo.GetSeatAsync(aircraftId, seatId, ct);
        if (seat is null) return null;

        seat.CabinClass = request.CabinClass;
        await _repo.SaveChangesAsync(ct);
        return ToSeatDto(seat);
    }

    public async Task<bool> DeleteSeatAsync(Guid aircraftId, Guid seatId, CancellationToken ct)
    {
        var seat = await _repo.GetSeatAsync(aircraftId, seatId, ct);
        if (seat is null) return false;

        await _repo.RemoveSeatAsync(seat, ct);
        return true;
    }

    private async Task EnsureAircraftExistsAsync(Guid id, CancellationToken ct)
    {
        if (await _repo.GetByIdAsync(id, ct) is null)
            throw new NotFoundException($"Aircraft '{id}' was not found.");
    }

    private static string NormalizeRegistration(string value)
        => value.Trim().ToUpperInvariant();

    private static AircraftResponseDto ToDto(Aircraft a, int seatsCount) => new()
    {
        Id = a.Id,
        RegistrationNumber = a.RegistrationNumber,
        Model = a.Model,
        IsActive = a.IsActive,
        SeatsCount = seatsCount
    };

    private static AircraftSeatResponseDto ToSeatDto(AircraftSeat s) => new()
    {
        Id = s.Id,
        SeatNumber = s.SeatNumber,
        CabinClass = s.CabinClass
    };
}
