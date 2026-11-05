namespace FlightBooking.Modules.Identity.Infrastructure.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FlightBooking.Modules.Identity.Application.DTOs;
using FlightBooking.Modules.Identity.Application.Interfaces;
using FlightBooking.Modules.Identity.Domain.Entities;
using FlightBooking.Modules.Identity.Domain.IRepositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class AuthService : IAuthService
{
    private const string CustomerRole = "Customer";

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var email = request.Email.Trim();

        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser != null)
            return Fail("Email is already registered!");


        if (request.DateOfBirth.Date >= DateTime.UtcNow.Date || (DateTime.UtcNow.Date.Year - request.DateOfBirth.Year) > 120)
            return Fail("Date of birth is invalid.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var (succeeded, errors) = await _userRepository.CreateUserAsync(user, request.Password);
        if (!succeeded)
            return Fail(string.Join(", ", errors));

        await _userRepository.AddToRoleAsync(user, CustomerRole);

        var jwtToken = await GenerateJwtTokenAsync(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshTokens.Add(refreshToken);
        await _userRepository.SaveChangesAsync();

        var roles = await _userRepository.GetUserRolesAsync(user);

        return MapToAuthResponse(
            user,
            jwtToken.Token,
            refreshToken.Token,
            jwtToken.Expires,
            roles,
            "User registered successfully!");
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
            return Fail("Invalid email or password!");

        var isPasswordValid = await _userRepository.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            return Fail("Invalid email or password!");

        var jwtToken = await GenerateJwtTokenAsync(user);

        var activeRefreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.IsActive);
        string refreshTokenValue;

        if (activeRefreshToken != null)
        {
            refreshTokenValue = activeRefreshToken.Token;
        }
        else
        {
            var newRefreshToken = GenerateRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
            await _userRepository.SaveChangesAsync();
            refreshTokenValue = newRefreshToken.Token;
        }

        var roles = await _userRepository.GetUserRolesAsync(user);

        return MapToAuthResponse(user, jwtToken.Token, refreshTokenValue, jwtToken.Expires, roles, "Login successful!");
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
        if (user == null)
            return Fail("Invalid refresh token");

        var storedToken = user.RefreshTokens.Single(rt => rt.Token == refreshToken);
        if (!storedToken.IsActive)
            return Fail("Refresh token is expired or revoked");

        storedToken.RevokedAtUtc = DateTime.UtcNow;

        var expiredTokens = user.RefreshTokens.Where(rt => !rt.IsActive).ToList();
        if (expiredTokens.Count != 0)
        {
            _refreshTokenRepository.RemoveRange(expiredTokens);
        }

        var newRefreshToken = GenerateRefreshToken();
        user.RefreshTokens.Add(newRefreshToken);
        await _userRepository.SaveChangesAsync();

        var jwtToken = await GenerateJwtTokenAsync(user);
        var roles = await _userRepository.GetUserRolesAsync(user);

        return MapToAuthResponse(user, jwtToken.Token, newRefreshToken.Token, jwtToken.Expires, roles, "Token refreshed successfully!");
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, string userId)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
        if (user == null || user.Id != userId) return false;

        var storedToken = user.RefreshTokens.Single(rt => rt.Token == refreshToken);
        if (!storedToken.IsActive) return false;

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        await _userRepository.SaveChangesAsync();

        return true;
    }

    // Helper methods

    private async Task<(string Token, DateTime Expires)> GenerateJwtTokenAsync(ApplicationUser user)
    {
        var userRoles = await _userRepository.GetUserRolesAsync(user);

        var authClaims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var jwtSecret = _configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
            throw new InvalidOperationException("Jwt:Secret must be configured and at least 32 bytes long.");

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

        var minutes = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var m) && m > 0 ? m : 120;
        var expires = DateTime.UtcNow.AddMinutes(minutes);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: expires,
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    private static RefreshToken GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomNumber),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static AuthResponseDto Fail(string message)
        => new() { IsAuthenticated = false, Message = message };

    private static AuthResponseDto MapToAuthResponse(
        ApplicationUser user, string token, string refreshToken,
        DateTime expiration, IList<string> roles, string message)
    {
        return new AuthResponseDto
        {
            IsAuthenticated = true,
            Message = message,
            Token = token,
            RefreshToken = refreshToken,
            Expiration = expiration,
            UserName = user.UserName!,
            Email = user.Email!,
            Roles = roles.ToList()
        };
    }

}
