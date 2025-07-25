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
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductItems)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Unit_Price = p.Unit_Price,
                    Selling_Price = p.Selling_Price,
                    Quantity = p.ProductItems.Count(pi => pi.Status == ProductItemStatus.InStock),
                    CategoryId = p.CategoryId,
                    Category = new CategoryDto
                    {
                        Id = p.CategoryId,
                        Name = p.Category.Name,
                        Description = p.Category.Description
                    },
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
            if (id <= 0)
                throw new ArgumentException("Invalid product ID.");

            var product = await _context.Products
                .Include(p  => p.ProductItems)
                .Where(p => p.Id == id)
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
                throw new KeyNotFoundException("Product not found");

            return Ok(new { success = true, data = product });
        }

        [HttpGet("names")]
        public async Task<IActionResult> GetProductNames()
        {
            var productNames = await _context.Products
                .Select(p => new ProductDropdownDto
                {
                    Id = p.Id,
                    Name = p.Name
                })
                .ToListAsync();

            return Ok(new { success = true, data = productNames });
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(CreateProductDto productDto)
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
        public async Task<IActionResult> EditProduct(int id, CreateProductDto productDto)
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

        [HttpGet("stock-summary")]
        public async Task<IActionResult> GetProductStock()
        {
            var userId = GetValidUserId();

            var productQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductItems)
                .AsQueryable();

            var summaries = await productQuery
                .Select(p => new ProductStockSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CategoryName = p.Category.Name,
                    QuantityInStock = p.ProductItems.Count(pi => pi.Status == ProductItemStatus.InStock)
                })
                .ToListAsync();

            return Ok(new { success = true, data = summaries });
        }
    }
}

