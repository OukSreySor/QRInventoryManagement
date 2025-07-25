using System.Linq;
using JwtAuth.Data;
using JwtAuth.Entity;
using JwtAuth.Models;
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
    public class CategoryController : BaseController
    {
        private readonly AppDbContext _context;
           
            public CategoryController(AppDbContext context)
            {
                _context = context;
            }

            [HttpGet]
            public async Task<IActionResult> GetCategories()
            {
                var categories = await _context.Categories
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        UserId = c.UserId
                    }).ToListAsync();

            return Ok(new { success = true, data = categories });
            }

            [HttpGet("{id}")]
            public async Task<IActionResult> GetCategory(int id)
            {
            if (id <= 0)
                throw new ArgumentException("Invalid category ID.");

            var category = await _context.Categories
                    .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    UserId = c.UserId
                })
                .FirstOrDefaultAsync();

                if (category == null)
                throw new KeyNotFoundException("Category not found or access denied.");

            return Ok(new { success = true, data = category });
            }

            [HttpGet("names")]
            public async Task<IActionResult> GetCategoryNames()
            {
                var categoryNames = await _context.Categories
                    .Select(p => new CategoryDropdownDto
                    {
                        Id = p.Id,
                        Name = p.Name
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = categoryNames });
            }

            [HttpPost]
            public async Task<IActionResult> CreateCategory(CategoryDto categoryDto)
            {
                var userId = GetValidUserId();
             
                var category = new Category
                {
                    Name = categoryDto.Name,
                    Description = categoryDto.Description,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, data = category });
            }

            [HttpPut("{id}")]
            public async Task<IActionResult> EditCategory(int id, CategoryDto categoryDto)
            {
                var userId = GetValidUserId();
                var userRole = GetValidUserRole();

                var categoryQuery = _context.Categories.Where(c => c.Id == id);

                if (userRole == "User")
                {
                    categoryQuery = categoryQuery.Where(c => c.UserId == userId);
                }
                var category = await categoryQuery.FirstOrDefaultAsync();

                if (category == null)
                {
                throw new KeyNotFoundException("Category not found or access denied.");
            }

                category.Name = categoryDto.Name;
                category.Description = categoryDto.Description;
                // Keep UserId as original owner
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Category updated successfully." });
            }

            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteCategory(int id)
            {
                var userId = GetValidUserId();
                var userRole = GetValidUserRole();

                var categoryQuery = _context.Categories.Where(c => c.Id == id);

                if (userRole == "User")
                {
                    categoryQuery = categoryQuery.Where(c => c.UserId == userId);
                }

                var category = await categoryQuery
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                    throw new KeyNotFoundException("Category not found or access denied.");

                if (category.Products.Any())
                throw new ArgumentException("Cannot delete a category that has products.");

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Category deleted successfully." });
            }
        }
    }



