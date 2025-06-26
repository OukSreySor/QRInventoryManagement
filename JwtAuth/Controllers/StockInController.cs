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
using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;


namespace JwtAuth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin, User")]
    public class StockInController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ProductService _productService;

        public StockInController(AppDbContext context, IWebHostEnvironment env, ProductService productService)
        {
            _context = context;
            _env = env;
            _productService = productService;
        }

        [HttpPost("stock-in")]
        public async Task<IActionResult> StockIn(StockInDto stockInDto)
        {
            var userId = GetValidUserId();

            var productItem = await _context.ProductItems.FirstOrDefaultAsync(pi => pi.Id == stockInDto.ProductItemId);
            if (productItem == null)
                throw new KeyNotFoundException($"ProductItem with Id {stockInDto.ProductItemId} not found.");

            if (productItem.Status == ProductItemStatus.InStock)
                throw new ArgumentException("Product item already stocked in.");

            if (stockInDto.ReceivedDate > DateTime.UtcNow)
                throw new ArgumentException("Received date cannot be in the future.");

            if (stockInDto.ReceivedDate < productItem.Manufacturing_Date)
                throw new ArgumentException("Received date cannot be before manufacturing date.");

            if (stockInDto.ReceivedDate > productItem.Expiry_Date)
                throw new ArgumentException("Received date cannot be after expiry date.");

            // Update item status to InStock
            productItem.Status = ProductItemStatus.InStock;

            // Create stock in record
            var stockIn = new StockIn
            {
                ProductItemId = stockInDto.ProductItemId,
                UserId = userId,
                ReceivedDate = stockInDto.ReceivedDate
            };

            await _context.StockIns.AddAsync(stockIn);

            // Transaction Record
            var transaction = new Transaction
            {
                ProductItemId = stockInDto.ProductItemId,
                UserId = userId,
                TransactionType = TransactionType.StockIn,
                TransactionDate = stockInDto.ReceivedDate
            };

            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            await _productService.UpdateProductStatusAsync(productItem.ProductId);

            return Ok(new { success = true, message = "Stock in recorded successfully."} );
        }

    }
}
