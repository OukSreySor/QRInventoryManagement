using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JwtAuth.Data;
using JwtAuth.Entity;
using JwtAuth.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JwtAuth.Services
{
    public class AuthService(AppDbContext context, IConfiguration configuration) : IAuthService
    {
        public async Task<TokenResponseDto?> LoginAsync(UserLoginDto request)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return null;
            }
            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
            {
                return null;
            }


            return await CreateTokenRefresh(user);
        }

        private async Task<TokenResponseDto> CreateTokenRefresh(User user)
        {
            return new TokenResponseDto
            {
                AccessToken = CreateToken(user),
                RefreshToken = await GenerateAndSaveRefreshTokenAsync(user),
            };
        }

        public async Task<User?> RegisterAsync(RegisterRequestDto request)
        {
            
            if (await context.Users.AnyAsync(u => u.Username == request.UserDto.Username || u.Email == request.UserDto.Email))
            {
                return null;
            }

            bool hasAnyUser = await context.Users.AnyAsync();

            var user = new User();
            var hashedPassword = new PasswordHasher<User>()
                .HashPassword(user, request.UserDto.Password);

            user.Username = request.UserDto.Username;
            user.Email = request.UserDto.Email;
            user.PasswordHash = hashedPassword;
            //user.Role = string.IsNullOrWhiteSpace(request.Role) ? "User" : request.Role;
            if (!hasAnyUser)
            {
                // First user becomes Admin (no invite code required)
                user.Role = "Admin";
            }
            else
            {
                // Must provide valid invite code
                var code = await context.InviteCodes.FirstOrDefaultAsync(c => c.Code == request.InviteCode && !c.IsUsed);
                if (code == null)
                {
                    throw new ArgumentException("Invalid or already used invite code.");
                }

                // Mark code as used
                code.IsUsed = true;
                user.Role = "User";
                code.UsedAt = DateTime.UtcNow;
            }
            context.Users.Add(user);
            await context.SaveChangesAsync();

            return user;
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);

            if (user is null)
                return null;

            return await CreateTokenRefresh(user);
        }
        private async Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
        {
            var user = await context.Users.FindAsync(userId);
            if(user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow) 
            { 
                return null; 
            }
            return user;
        }
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);

        }

        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
        {
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await context.SaveChangesAsync();

            return refreshToken;
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)

            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: configuration.GetValue<string>("AppSettings:Issuer"),
                audience: configuration.GetValue<string>("AppSettings:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        }
        public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
        {
            return await context.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserProfileDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

    }
}
