using JwtAuth.Data;
using JwtAuth.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtAuth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Role,
                    u.CreatedAt,
                }).ToListAsync();
            return Ok( new { success = true, data = users });
        }
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            return Ok(new
            {
                success = true,
                data = new 
                {
                    user.Id,
                    user.Username,
                    user.Role,
                    user.CreatedAt
                }
                
            });
        }
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUserRole(Guid id, UpdateUserRoleDto updateDto)
        {
            if (id != updateDto.UserId)
                throw new ArgumentException("ID does not match");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            if (updateDto.NewRole != "Admin" && updateDto.NewRole != "User")
                throw new ArgumentException("Invalid role. Only 'Admin' or 'User' allowed.");

            user.Role = updateDto.NewRole;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    user.Id,
                    user.Username,
                    user.Role
                }
            });
        }
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "User deleted successfully." });
        }
    }

    public class UpdateUserRoleDto
    {
        public required Guid UserId { get; set; }
        public required string NewRole { get; set; }
    }

}
