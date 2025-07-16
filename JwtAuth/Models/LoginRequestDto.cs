using System.ComponentModel.DataAnnotations.Schema;

namespace JwtAuth.Models
{
    [NotMapped]
    public class LoginRequestDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
