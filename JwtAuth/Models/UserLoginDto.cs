using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuth.Models
{
    [NotMapped]
    public class UserLoginDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
