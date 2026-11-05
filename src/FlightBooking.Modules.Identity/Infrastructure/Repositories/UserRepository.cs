using FlightBooking.Modules.Identity.Domain.Entities;
using FlightBooking.Modules.Identity.Domain.IRepositories;
using FlightBooking.Modules.Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FlightBooking.Modules.Identity.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityModuleDbContext _context;

    public UserRepository(UserManager<ApplicationUser> userManager, IdentityModuleDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        var normalized = _userManager.NormalizeEmail(email.Trim());
        return _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized);
    }

    public Task<ApplicationUser?> GetByRefreshTokenAsync(string refreshToken)
        => _context.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken));

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(ApplicationUser user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);
        return (result.Succeeded, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task AddToRoleAsync(ApplicationUser user, string role)
    {
        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to assign role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }

    public Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        => _userManager.GetRolesAsync(user);

    public Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
        => _userManager.CheckPasswordAsync(user, password);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
