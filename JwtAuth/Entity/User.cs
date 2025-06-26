using Microsoft.Identity.Client;

namespace JwtAuth.Entity
{
    public class User: BaseEntity
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty ;
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
        public string? RefreshToken {  get; set; }  
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
