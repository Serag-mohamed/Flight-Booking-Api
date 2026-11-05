using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlightBooking.Modules.Reporting.Application.DTOs;
using FlightBooking.Modules.Reporting.Application.Interfaces;
using FlightBooking.Modules.Reporting.Domain.Entities;
using FlightBooking.Modules.Reporting.Infrastructure.Data;
using FlightBooking.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Modules.Reporting.Infrastructure.Services;

public class ReportService : IReportService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const int MaxRangeDays = 366;
    private const int MaxOccupancyFlights = 500;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ReportingDbContext _db;

    public ReportService(ReportingDbContext db) => _db = db;

    public Task<RevenueReportDto> GetRevenueAsync(ReportQueryParams query, CancellationToken ct)
    {
        var (from, to) = ResolveRange(query);
        var currency = query.Currency.Trim().ToUpperInvariant();

        return CachedAsync("revenue", $"{from:O}|{to:O}|{currency}", async () =>
        {
            var (fromUtc, toUtc) = ToUtcBounds(from, to);

            var payments = await _db.Payments.AsNoTracking()
                .Where(p => p.Currency == currency
                            && (p.Status == "Succeeded" || p.Status == "Refunded")
                            && p.CreatedAtUtc >= fromUtc && p.CreatedAtUtc < toUtc)
                .Select(p => new { p.CreatedAtUtc, p.Amount })
                .ToListAsync(ct);

            var refunds = await (
                    from r in _db.Refunds.AsNoTracking()
                    join p in _db.Payments.AsNoTracking() on r.PaymentId equals p.Id
                    where p.Currency == currency
                          && r.Status == "Succeeded"
                          && r.CreatedAtUtc >= fromUtc && r.CreatedAtUtc < toUtc
                    select new { r.CreatedAtUtc, r.Amount })
                .ToListAsync(ct);

            var revenueByDay = payments.GroupBy(x => DateOnly.FromDateTime(x.CreatedAtUtc.UtcDateTime))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
            var refundsByDay = refunds.GroupBy(x => DateOnly.FromDateTime(x.CreatedAtUtc.UtcDateTime))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var daily = new List<DailyRevenueDto>();
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                var revenue = revenueByDay.GetValueOrDefault(day);
                var refunded = refundsByDay.GetValueOrDefault(day);
                daily.Add(new DailyRevenueDto { Date = day, Revenue = revenue, Refunds = refunded, Net = revenue - refunded });
            }

            var totalRevenue = daily.Sum(d => d.Revenue);
            var totalRefunds = daily.Sum(d => d.Refunds);

            return new RevenueReportDto
            {
                From = from,
                To = to,
                Currency = currency,
                TotalRevenue = totalRevenue,
                TotalRefunds = totalRefunds,
                NetRevenue = totalRevenue - totalRefunds,
                Daily = daily
            };
        }, ct);
    }

    public Task<BookingsByStatusReportDto> GetBookingsByStatusAsync(ReportQueryParams query, CancellationToken ct)
    {
        var (from, to) = ResolveRange(query);

        return CachedAsync("bookings-by-status", $"{from:O}|{to:O}", async () =>
        {
            var (fromUtc, toUtc) = ToUtcBounds(from, to);

            var rows = await _db.Bookings.AsNoTracking()
                .Where(b => b.CreatedAtUtc >= fromUtc && b.CreatedAtUtc < toUtc)
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(b => b.TotalAmount) })
                .ToListAsync(ct);

            var statuses = rows
                .OrderByDescending(r => r.Count)
                .Select(r => new BookingStatusCountDto { Status = r.Status, Count = r.Count, TotalAmount = r.Total })
                .ToList();

            return new BookingsByStatusReportDto
            {
                From = from,
                To = to,
                TotalBookings = statuses.Sum(s => s.Count),
                Statuses = statuses
            };
        }, ct);
    }

    public Task<TopRoutesReportDto> GetTopRoutesAsync(ReportQueryParams query, CancellationToken ct)
    {
        var (from, to) = ResolveRange(query);
        var currency = query.Currency.Trim().ToUpperInvariant();
        var top = Math.Clamp(query.Top, 1, 50);

        return CachedAsync("top-routes", $"{from:O}|{to:O}|{currency}|{top}", async () =>
        {
            var (fromUtc, toUtc) = ToUtcBounds(from, to);

            var rows = await _db.Bookings.AsNoTracking()
                .Where(b => b.Status == "Confirmed" && b.Currency == currency
                            && b.CreatedAtUtc >= fromUtc && b.CreatedAtUtc < toUtc)
                .GroupBy(b => new { b.OriginIata, b.DestinationIata })
                .Select(g => new { g.Key.OriginIata, g.Key.DestinationIata, Count = g.Count(), Revenue = g.Sum(b => b.TotalAmount) })
                .OrderByDescending(r => r.Revenue)
                .Take(top)
                .ToListAsync(ct);

            return new TopRoutesReportDto
            {
                From = from,
                To = to,
                Currency = currency,
                Routes = rows.Select(r => new TopRouteDto
                {
                    Origin = r.OriginIata,
                    Destination = r.DestinationIata,
                    Bookings = r.Count,
                    Revenue = r.Revenue
                }).ToList()
            };
        }, ct);
    }

    public Task<FlightOccupancyReportDto> GetFlightOccupancyAsync(ReportQueryParams query, CancellationToken ct)
    {
        var (from, to) = ResolveRange(query);

        return CachedAsync("flight-occupancy", $"{from:O}|{to:O}", async () =>
        {
            var (fromUtc, toUtc) = ToUtcBounds(from, to);

            var flights = await _db.Flights.AsNoTracking()
                .Where(f => f.DepartureUtc >= fromUtc && f.DepartureUtc < toUtc && f.Status != "Cancelled")
                .OrderBy(f => f.DepartureUtc)
                .Select(f => new { f.Id, f.FlightNumber, f.DepartureUtc })
                .Take(MaxOccupancyFlights)
                .ToListAsync(ct);

            var ids = flights.Select(f => f.Id).ToList();

            var counts = await _db.FlightSeats.AsNoTracking()
                .Where(s => ids.Contains(s.FlightId))
                .GroupBy(s => new { s.FlightId, s.Status })
                .Select(g => new { g.Key.FlightId, g.Key.Status, Count = g.Count() })
                .ToListAsync(ct);

            var byFlight = counts.ToLookup(c => c.FlightId);

            var result = flights.Select(f =>
            {
                int Count(string status) => byFlight[f.Id].Where(c => c.Status == status).Sum(c => c.Count);

                var booked = Count("Booked");
                var held = Count("Held");
                var unavailable = Count("Unavailable");
                var available = Count("Available");
                var total = booked + held + unavailable + available;

                return new FlightOccupancyDto
                {
                    FlightId = f.Id,
                    FlightNumber = f.FlightNumber,
                    DepartureUtc = f.DepartureUtc,
                    TotalSeats = total,
                    BookedSeats = booked,
                    HeldSeats = held,
                    UnavailableSeats = unavailable,
                    AvailableSeats = available,
                    OccupancyPercent = total == 0 ? 0 : Math.Round(booked * 100.0 / total, 1)
                };
            }).ToList();

            return new FlightOccupancyReportDto { From = from, To = to, Flights = result };
        }, ct);
    }

    private static (DateOnly From, DateOnly To) ResolveRange(ReportQueryParams query)
    {
        var to = query.To ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var from = query.From ?? to.AddDays(-29);

        if (from > to)
            throw new BusinessRuleException("'From' must be on or before 'To'.");

        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
            throw new BusinessRuleException($"The report range cannot exceed {MaxRangeDays} days.");

        return (from, to);
    }

    private static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) ToUtcBounds(DateOnly from, DateOnly to)
        => (new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

    private async Task<T> CachedAsync<T>(string scope, string args, Func<Task<T>> factory, CancellationToken ct)
        where T : class
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(args)))[..32];

        var cached = await _db.GeneratedReports.FirstOrDefaultAsync(r => r.Scope == scope && r.Key == key, ct);

        if (cached is not null && DateTimeOffset.UtcNow - cached.CreatedAtUtc < CacheTtl)
        {
            var hit = JsonSerializer.Deserialize<T>(cached.ResponseBody, Json);
            if (hit is not null) return hit;
        }

        var result = await factory();
        var body = JsonSerializer.Serialize(result, Json);

        if (cached is null)
        {
            _db.GeneratedReports.Add(new GeneratedReport
            {
                Scope = scope,
                Key = key,
                RequestHash = args.Length > 255 ? args[..255] : args,
                ResponseBody = body
            });
        }
        else
        {
            cached.ResponseBody = body;
            cached.RequestHash = args.Length > 255 ? args[..255] : args;
            cached.CreatedAtUtc = DateTimeOffset.UtcNow;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
        }

        return result;
    }
}
