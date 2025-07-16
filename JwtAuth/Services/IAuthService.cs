using JwtAuth.Entity;
using JwtAuth.Models;

namespace JwtAuth.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(RegisterRequestDto request);

        Task<TokenResponseDto?> LoginAsync(LoginRequestDto request);

        Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<UserProfileDto?> GetUserProfileAsync(Guid userId);

    }

}
