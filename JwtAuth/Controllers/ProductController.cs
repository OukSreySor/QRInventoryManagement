using JwtAuth.Data;
using JwtAuth.Entity;
using JwtAuth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtAuth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin, User")]
    public class ProductController : BaseController
    {
        private readonly AppDbContext context;
        public ProductController(AppDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public IActionResult GetProducts()
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            // Start building the query
            var productsQuery = context.Products
                .Include(p => p.ProductItems)
                .AsQueryable();

            // If the user is not an admin, filter products by their own UserId
            if (userRole != "Admin")
            {
                productsQuery = productsQuery.Where(p => p.UserId == userId);
            }

            var products = productsQuery
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Image = p.Image,
                Unit_Price = p.Unit_Price,
                Selling_Price = p.Selling_Price,
                Quantity = p.ProductItems.Count(pi => pi.Status == "InStock"),
                CategoryId = p.CategoryId,
                UserId = p.UserId,
                ProductItems = p.ProductItems.Select(pi => new ProductItemDto
                {
                    Id = pi.Id,
                    QR_Code = pi.QR_Code,
                    Serial_Number = pi.Serial_Number,
                    Status = pi.Status,
                    ProductId = pi.ProductId,
                    UserId = pi.UserId

                }).ToList()
            }).ToList();

            return Ok(products);
        }

        [HttpGet("{id}")]
        public IActionResult GetProduct(int id)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var productQuery = context.Products
                .Include(p => p.ProductItems)
                .Where(p => p.Id == id);

            if (userRole == "User")
            {
                productQuery = productQuery.Where(p => p.UserId == userId);
            }
            var product = productQuery
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Image = p.Image,
                    Unit_Price = p.Unit_Price,
                    Selling_Price = p.Selling_Price,
                    Quantity = p.ProductItems.Count(pi => pi.Status == "InStock"),
                    CategoryId = p.CategoryId,
                    UserId = p.UserId,
                    ProductItems = p.ProductItems
                        .Select(pi => new ProductItemDto
                        {
                            Id = pi.ProductId,
                            QR_Code = pi.QR_Code,
                            Serial_Number = pi.Serial_Number,
                            Status = pi.Status,
                            ProductId = pi.ProductId,
                            UserId = pi.UserId
                        }).ToList()
                }).FirstOrDefault();

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }
        [HttpPost]
        public IActionResult CreateProduct(ProductDto productDto)
        {
            var categoryExists = context.Categories.Any(c => c.Id == productDto.CategoryId);
            var userId = GetValidUserId();
            
            if (!categoryExists)
                return BadRequest("Invalid CategoryId.");

            var product = new Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                Image = productDto.Image,
                Unit_Price = productDto.Unit_Price,
                Selling_Price = productDto.Selling_Price,
                CategoryId = productDto.CategoryId,
                UserId = userId,
                CreatedAt = DateTime.Now,
            };

            context.Products.Add(product);
            context.SaveChanges();

            return Ok(product);

        }
        [HttpPut("{id}")]
        public IActionResult EditProduct(int id, ProductDto productDto)
        {
            var userId = GetValidUserId();
            var product = context.Products.FirstOrDefault(p => p.Id == id && p.UserId == userId);

            if (product == null)
            {
                return NotFound();
            }

            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.Image = productDto.Image;
            product.Unit_Price = productDto.Unit_Price;
            product.Selling_Price = productDto.Selling_Price;
            product.CategoryId = productDto.CategoryId;
           
            context.SaveChanges();

            return Ok(product);

        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(int id) 
        {
            var userId = GetValidUserId();
            var product = context.Products.FirstOrDefault(p => p.Id == id && p.UserId == userId);

            if (product == null)
            {
                return NotFound();
            }

            context.Products.Remove(product);
            context.SaveChanges();

            return Ok("Product delete success.");
            
                
        }
    }
}

