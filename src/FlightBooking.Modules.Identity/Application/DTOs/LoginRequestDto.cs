using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Modules.Identity.Application.DTOs;

public record LoginRequestDto
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
