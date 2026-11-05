using FlightBooking.Modules.Identity.Domain.Entities;

namespace FlightBooking.Modules.Identity.Domain.IRepositories;

public interface IRefreshTokenRepository
{
    void RemoveRange(IEnumerable<RefreshToken> tokens);
}
