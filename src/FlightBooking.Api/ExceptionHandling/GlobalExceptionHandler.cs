using FlightBooking.Shared.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Api.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            NotFoundException            => (StatusCodes.Status404NotFound, "Not Found"),
            ConflictException            => (StatusCodes.Status409Conflict, "Conflict"),
            BusinessRuleException        => (StatusCodes.Status422UnprocessableEntity, "Business Rule Violation"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency Conflict"),
            _                            => (StatusCodes.Status500InternalServerError, "Server Error")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception");
        else
            _logger.LogWarning("{Type}: {Message}", exception.GetType().Name, exception.Message);

        var detail = exception switch
        {
            DbUpdateConcurrencyException => "The record was modified by another request. Please reload and try again.",
            _ when status == StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
            _ => exception.Message
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
