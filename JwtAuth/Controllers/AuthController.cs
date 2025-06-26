using JwtAuth.Models;
using JwtAuth.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using JwtAuth.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using JwtAuth.Helpers;

namespace JwtAuth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : BaseController
    {
        public static User user = new();
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            var user = await authService.RegisterAsync(request);
            if (user == null)
                throw new ArgumentException("Username already exists.");

            return Ok(new { success = true, data = user });
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDto>> Login(UserLoginDto request)
        {
            var result = await authService.LoginAsync(request);
            if (result == null)
                throw new UnauthorizedAccessException("Invalid email or password.");

            return Ok(new { success = true, data = result });
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken(RefreshTokenRequestDto request)
        {
            var result = await authService.RefreshTokenAsync(request);
            if (result is null || result.AccessToken is null || result.RefreshToken is null)
                throw new UnauthorizedAccessException("Invalid refresh token.");

            return Ok(new { success = true, data = result });
        }

        [Authorize]
        [HttpGet]
        public IActionResult AuthenticatedOnlyEndpoint()
        {
            return Ok(new { success = true, message = "You are authenticated." });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnlyEndpoint()
        {
            return Ok(new { success = true, message = "You are an admin." });
        }

        [Authorize(Roles = "User")]
        [HttpGet("user-only")]
        public IActionResult UserOnlyEndpoint()
        {
            return Ok(new { success = true, message = "You are a user." });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetValidUserId();

            var user = await authService.GetUserProfileAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            return Ok(new { success = true, data = user });

        }
    }
}
