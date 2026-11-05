namespace FlightBooking.Modules.Identity.Application.DTOs;

public record AuthResponseDto
{
    public bool IsAuthenticated { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Token { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? Expiration { get; init; }
    public string? UserName { get; init; }
    public string? Email { get; init; }
    public List<string> Roles { get; init; } = new();
}
