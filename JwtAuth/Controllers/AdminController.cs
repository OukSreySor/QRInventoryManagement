using JwtAuth.Data;
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
        private readonly AppDbContext context;

        public AdminController(AppDbContext context)
        {
            this.context = context;
        }
        [HttpGet("users")]
        public IActionResult GetAllUsers()
        {
            var users = context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Role,
                    u.CreatedAt,
                }).ToList();
            return Ok(users);
        }
        [HttpGet("users/{id}")]
        public IActionResult GetUserById(Guid id)
        {
            var user = context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Role,
                user.CreatedAt
            });
        }
        [HttpPut("users/{id}")]
        public IActionResult UpdateUserRole(Guid id, UpdateUserRoleDto updateDto)
        {
            if (id != updateDto.UserId)
                return BadRequest("ID does not match!");

            var user = context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            if (updateDto.NewRole != "Admin" && updateDto.NewRole != "User")
                return BadRequest("Invalid role. Only 'Admin' or 'User' allowed.");

            user.Role = updateDto.NewRole;
            context.SaveChanges();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Role
            });
        }
        [HttpDelete("users/{id}")]
        public IActionResult DeleteUser(Guid id)
        {
            var user = context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            context.Users.Remove(user);
            context.SaveChanges();

            return Ok("User deleted successfully.");
        }
    }

    public class UpdateUserRoleDto
    {
        public required Guid UserId { get; set; }
        public required string NewRole { get; set; }
    }

}
