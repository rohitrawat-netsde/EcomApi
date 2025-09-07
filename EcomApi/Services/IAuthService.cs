using EcomApi.DTOs;

namespace EcomApi.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string ipAddress);
        Task<AuthResponseDto> LoginAsync(LoginDto dto, string ipAddress);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshTokenRaw, string ipAddress);
        Task RevokeRefreshTokenAsync(string refreshTokenRaw);
    }
}
