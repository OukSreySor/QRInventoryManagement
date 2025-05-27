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
    [Authorize(Roles = "Admin, User")]
    public class CategoryController : BaseController
    {
        private readonly AppDbContext context;
           
            public CategoryController(AppDbContext context)
            {
                this.context = context;
            }

            [HttpGet]
            public IActionResult GetCategories()
            {
                var userId = GetValidUserId();
                var userRole = GetValidUserRole();

                var categoriesQuery = context.Categories.AsQueryable();

                if (userRole == "User")
                {
                    categoriesQuery = categoriesQuery.Where(c => c.UserId == userId);
                }

                var categories = categoriesQuery
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        UserId = c.UserId  
                    }).ToList();

            return Ok(categories);
            }

            [HttpGet("{id}")]
            public IActionResult GetCategory(int id)
            {
                var userId = GetValidUserId();
                var userRole = GetValidUserRole();

                var categoryQuery = context.Categories.Where(c => c.Id == id);

                if (userRole == "User")
                {
                    categoryQuery = categoryQuery.Where(c => c.UserId == userId);
                }
                var category = categoryQuery
                    .Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    UserId = c.UserId
                })
                .FirstOrDefault();

                if (category == null)
                    {
                        return NotFound("Category not found or access denied.");
                    }

                    return Ok(category);
            
                }

            [HttpPost]
            public IActionResult CreateCategory(CategoryDto categoryDto)
            {
                var userId = GetValidUserId();
             
                var category = new Category
                {
                    Name = categoryDto.Name,
                    Description = categoryDto.Description,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };

                context.Categories.Add(category);
                context.SaveChanges();

                return Ok(category);
            }

            [HttpPut("{id}")]
            public IActionResult EditCategory(int id, CategoryDto categoryDto)
            {
                var userId = GetValidUserId();

                var category = context.Categories
                    .FirstOrDefault(c => c.Id == id && c.UserId == userId);

                if (category == null)
                {
                    return NotFound("Category not found or access denied.");
                }

                category.Name = categoryDto.Name;
                category.Description = categoryDto.Description;
                category.UserId = userId;

                context.SaveChanges();
                return Ok("Category updated successfully.");
            }

            [HttpDelete("{id}")]
            public IActionResult DeleteCategory(int id)
            {
                var userId = GetValidUserId();

                var category = context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefault(c => c.Id == id && c.UserId == userId);

                if (category == null)
                    return NotFound("Category not found or access denied.");

                if (category.Products.Any())
                    return BadRequest("Cannot delete a category that has products.");

                context.Categories.Remove(category);
                context.SaveChanges();
                return Ok("Category deleted successfully.");
            }
        }
    }



