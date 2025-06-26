using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuth.Models
{
    [NotMapped]
    public class UserProfileDto
    {
        public required Guid Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public required DateTime CreatedAt { get; set; }

    }
}
