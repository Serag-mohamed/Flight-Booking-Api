using FlightBooking.Modules.Identity.Domain.Entities;

namespace FlightBooking.Modules.Identity.Domain.IRepositories;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(string email);

    Task<ApplicationUser?> GetByRefreshTokenAsync(string refreshToken);

    Task<(bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(ApplicationUser user, string password);
    Task AddToRoleAsync(ApplicationUser user, string role);
    Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
    Task SaveChangesAsync();
}
