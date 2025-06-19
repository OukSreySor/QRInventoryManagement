using JwtAuth.Data;
using JwtAuth.Entity;
using JwtAuth.Entity.Enums;
using JwtAuth.Helpers;
using JwtAuth.Models;
using JwtAuth.Services;
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
        private readonly AppDbContext _context;
        private readonly ProductService _productService;
        public ProductController(AppDbContext context, ProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            // Start building the query
            var productsQuery = _context.Products
                .Include(p => p.ProductItems)
                .AsQueryable();

            // If the user is not an admin, filter products by their own UserId
            if (userRole != "Admin")
            {
                productsQuery = productsQuery.Where(p => p.UserId == userId);
            }

            var products = await productsQuery
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Image = p.Image,
                Unit_Price = p.Unit_Price,
                Selling_Price = p.Selling_Price,
                Quantity = p.ProductItems.Count(pi => pi.Status == ProductItemStatus.InStock),
                CategoryId = p.CategoryId,
                UserId = p.UserId,
                Status = p.Status,
                ProductItems = p.ProductItems.Select(pi => new ProductItemDto
                {
                    Id = pi.Id,
                    QR_Code = pi.QR_Code,
                    Serial_Number = pi.Serial_Number,
                    Status = pi.Status,
                    ProductId = pi.ProductId,
                    UserId = pi.UserId

                }).ToList()
            }).ToListAsync();

            return Ok(new { success = true, data = products });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var productQuery = _context.Products
                .Include(p => p.ProductItems)
                .Where(p => p.Id == id);

            if (userRole == "User")
            {
                productQuery = productQuery.Where(p => p.UserId == userId);
            }

            if (id <= 0)
                throw new ArgumentException("Invalid product ID.");

            var product = await productQuery
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Image = p.Image,
                    Unit_Price = p.Unit_Price,
                    Selling_Price = p.Selling_Price,
                    Quantity = p.ProductItems.Count(pi => pi.Status == ProductItemStatus.InStock),
                    CategoryId = p.CategoryId,
                    UserId = p.UserId,
                    Status = p.Status,  
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
                }).FirstOrDefaultAsync();

            if (product == null)
                throw new KeyNotFoundException("Product not found or access denied.");

            return Ok(new { success = true, data = product });
        }
        [HttpPost]
        public async Task<IActionResult> CreateProduct(ProductDto productDto)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            bool categoryExists;
            if (userRole == "User")
            {
                categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == productDto.CategoryId && c.UserId == userId);
            }
            else if (userRole == "Admin")
            {
                categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == productDto.CategoryId);
            }
            else
            {
                throw new UnauthorizedAccessException("Invalid user role.");
            }

            if (!categoryExists)
                throw new ArgumentException("Invalid CategoryId or access denied.");
            
            if (productDto.Unit_Price <= 0)
                throw new ArgumentException("Unit price must be a positive number.");

            if (productDto.Selling_Price < productDto.Unit_Price)
                throw new ArgumentException("Selling price must be greater than or equal to unit price.");

            var isDuplicate = await _context.Products.AnyAsync(p =>
                p.Name == productDto.Name.Trim() && p.UserId == userId);

            if (isDuplicate)
                throw new ArgumentException("Product with the same name already exists.");

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

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            await _productService.UpdateProductStatusAsync(product.Id);
            return Ok(new { success = true, data = product });

        }
        [HttpPut("{id}")]
        public async Task<IActionResult> EditProduct(int id, ProductDto productDto)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var productQuery = _context.Products.Where(p => p.Id == id);

            if(userRole == "User")
            {
                productQuery = productQuery.Where(p => p.UserId == userId);
            }

            if (id <= 0)
                throw new ArgumentException("Invalid product ID.");

            // Category validation based on role
            if (userRole == "User")
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == productDto.CategoryId && c.UserId == userId);

                if (!categoryExists)
                    throw new UnauthorizedAccessException("Invalid CategoryId or access denied.");
            }
            else if (userRole == "Admin")
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == productDto.CategoryId);

                if (!categoryExists)
                    throw new ArgumentException("Invalid category. Please select a valid category.");
            }

            var product = await productQuery.FirstOrDefaultAsync();

            if (product == null)
                throw new KeyNotFoundException("Product not found or access denied.");

            if (productDto.Unit_Price <= 0)
                throw new ArgumentException("Unit price must be a positive number.");

            if (productDto.Selling_Price < productDto.Unit_Price)
                throw new ArgumentException("Selling price must be greater than or equal to unit price.");

            var isDuplicate = await _context.Products.AnyAsync(p =>
                p.Name == productDto.Name.Trim() && p.UserId == userId && p.Id != id);

            if (isDuplicate)
                throw new ArgumentException("Product with the same name already exists.");

            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.Image = productDto.Image;
            product.Unit_Price = productDto.Unit_Price;
            product.Selling_Price = productDto.Selling_Price;
            product.CategoryId = productDto.CategoryId;
            if (productDto.Status == ProductStatus.Discontinued)
            {
                product.Status = ProductStatus.Discontinued;
            }
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product updated successfully." });

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id) 
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var productQuery = _context.Products.Where(p => p.Id == id);

            if (userRole == "User")
            {
                productQuery = productQuery.Where(p => p.UserId == userId);
            }
            if (id <= 0)
                throw new ArgumentException("Invalid product ID.");

            var product = await productQuery.FirstOrDefaultAsync();
            if (product == null)
                throw new KeyNotFoundException("Product not found or access denied.");

            var hasItems = await _context.ProductItems.AnyAsync(pi => pi.ProductId == product.Id);
            if (hasItems)
                throw new InvalidOperationException("Cannot delete product with existing stock items.");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product deleted successfully." });      
        }
    }
}

