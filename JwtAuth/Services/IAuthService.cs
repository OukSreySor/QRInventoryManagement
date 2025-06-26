using JwtAuth.Entity;
using JwtAuth.Models;

namespace JwtAuth.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(UserDto request);

        Task<TokenResponseDto?> LoginAsync(UserLoginDto request);

        Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<UserProfileDto?> GetUserProfileAsync(Guid userId);

    }

}
