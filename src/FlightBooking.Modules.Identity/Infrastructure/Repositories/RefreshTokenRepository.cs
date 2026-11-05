using FlightBooking.Modules.Identity.Domain.Entities;
using FlightBooking.Modules.Identity.Domain.IRepositories;
using FlightBooking.Modules.Identity.Infrastructure.Data;

namespace FlightBooking.Modules.Identity.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityModuleDbContext _context;

    public RefreshTokenRepository(IdentityModuleDbContext context) => _context = context;

    public void RemoveRange(IEnumerable<RefreshToken> tokens)
        => _context.RefreshTokens.RemoveRange(tokens);
}
