using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Modules.Identity.Application.DTOs;

public record RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
