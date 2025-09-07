using EcomApi.Entities;

namespace EcomApi.Services
{
    public interface IJwtService
    {
        string GenerateAccessToken(ApplicationUser user);
        string CreateRefreshTokenRaw(); // returns raw string
        int GetAccessTokenExpiryMinutes();
        int GetRefreshTokenExpiryDays();
    }
}
