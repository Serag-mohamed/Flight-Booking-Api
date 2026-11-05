using FlightBooking.Shared.Common;
using FlightBooking.Modules.Flights.Application.DTOs;
using FlightBooking.Modules.Flights.Application.Interfaces;
using FlightBooking.Modules.Flights.Domain.Entities;
using FlightBooking.Modules.Flights.Domain.IRepositories;
using FlightBooking.Shared.Exceptions;

namespace FlightBooking.Modules.Flights.Infrastructure.Services;

public class AirportService : IAirportService
{
    private const int MaxPageSize = 100;
    private readonly IAirportRepository _airportRepository;

    public AirportService(IAirportRepository airportRepository)
    {
        _airportRepository = airportRepository;
    }

    public async Task<PagedResult<AirportResponseDto>> GetAllAsync(
        AirportQueryParams query, CancellationToken ct)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var (items, total) = await _airportRepository.GetPagedAsync(
            query.Search?.Trim(), page, pageSize, ct);

        return new PagedResult<AirportResponseDto>(
            Items: items.Select(a => a.ToDto()).ToList(),
            Page: page,
            PageSize: pageSize,
            TotalCount: total);
    }

    public async Task<AirportResponseDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var airport = await _airportRepository.GetByIdAsync(id, ct);
        return airport?.ToDto();
    }

    public async Task<AirportResponseDto?> GetByIataCodeAsync(string iataCode, CancellationToken ct)
    {
        var airport = await _airportRepository.GetByIataCodeAsync(
            iataCode.Trim().ToUpperInvariant(), ct);
        return airport?.ToDto();
    }

    public async Task<AirportResponseDto> AddAsync(AddAirportRequestDto request, CancellationToken ct)
    {
        var iataCode = request.IataCode.Trim().ToUpperInvariant();
        var timeZoneId = request.TimeZoneId.Trim();

        EnsureValidTimeZone(timeZoneId);

        if (await _airportRepository.IataCodeExistsAsync(iataCode, excludeId: null, ct))
            throw new ConflictException($"Airport with IATA code '{iataCode}' already exists.");

        var entity = new Airport
        {
            IataCode = iataCode,
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            Country = request.Country.Trim(),
            TimeZoneId = timeZoneId
        };

        await _airportRepository.AddAsync(entity, ct);
        return entity.ToDto();
    }

    public async Task<AirportResponseDto?> UpdateAsync(
        Guid id, UpdateAirportRequestDto request, CancellationToken ct)
    {
        var airport = await _airportRepository.GetByIdAsync(id, ct);
        if (airport is null) return null;

        var iataCode = request.IataCode.Trim().ToUpperInvariant();
        var timeZoneId = request.TimeZoneId.Trim();

        EnsureValidTimeZone(timeZoneId);

        if (await _airportRepository.IataCodeExistsAsync(iataCode, excludeId: id, ct))
            throw new ConflictException($"Airport with IATA code '{iataCode}' already exists.");

        airport.IataCode = iataCode;
        airport.Name = request.Name.Trim();
        airport.City = request.City.Trim();
        airport.Country = request.Country.Trim();
        airport.TimeZoneId = timeZoneId;

        await _airportRepository.SaveChangesAsync(ct);
        return airport.ToDto();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var airport = await _airportRepository.GetByIdAsync(id, ct);
        if (airport is null) return false;

        if (await _airportRepository.HasFlightsAsync(id, ct))
            throw new ConflictException("Cannot delete an airport that is linked to flights.");

        await _airportRepository.DeleteAsync(airport, ct);
        return true;
    }

    private static void EnsureValidTimeZone(string timeZoneId)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new BusinessRuleException($"'{timeZoneId}' is not a valid time zone.");
        }
    }
}
