using System.ComponentModel.DataAnnotations;

namespace FlightBooking.Modules.Identity.Application.DTOs;

public record RegisterRequestDto
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; init; }

    [Required]
    public DateTime DateOfBirth { get; init; }
}
