namespace JwtAuth.Models
{
    public class RegisterRequestDto
    {
        public UserDto UserDto { get; set; } = new UserDto
        {
            Username = string.Empty,
            Email = string.Empty,
            Password = string.Empty,
        };
        public string InviteCode { get; set; } = string.Empty;
    }
}
