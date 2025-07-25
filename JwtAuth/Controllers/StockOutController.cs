using System.Text;
using JwtAuth.Data;
using JwtAuth.Entity;
using JwtAuth.Entity.Enums;
using JwtAuth.Helpers;
using JwtAuth.Models;
using JwtAuth.Models.JwtAuth.Models;
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
    public class StockOutController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly ProductService _productService;
        public StockOutController(AppDbContext context, ProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        [HttpPost("stock-out")]
        public async Task<IActionResult> StockOut(StockOutDto stockOutDto)
        {
            var userId = GetValidUserId(); 

            var productItem = await _context.ProductItems.FirstOrDefaultAsync(pi => pi.Id == stockOutDto.ProductItemId);
            if (productItem == null)
                throw new KeyNotFoundException($"ProductItem with Id {stockOutDto.ProductItemId} not found.");

            if (productItem.Status != ProductItemStatus.InStock)
                throw new ArgumentException("ProductItem is not currently in stock.");

            if (stockOutDto.SoldDate > DateTime.UtcNow)
                throw new ArgumentException("Sold date cannot be in the future.");

            var isAlreadySold = await _context.StockOuts
                .AnyAsync(so => so.ProductItemId == stockOutDto.ProductItemId);

            if (isAlreadySold)
                throw new InvalidOperationException("This product item has already been sold.");

            var stockIn = await _context.StockIns
                .FirstOrDefaultAsync(si => si.ProductItemId == stockOutDto.ProductItemId);

            if (stockIn == null)
                throw new InvalidOperationException("This product item hasn't been stocked in yet.");

            if (stockOutDto.SoldDate < stockIn.ReceivedDate)
                throw new ArgumentException("Sold date cannot be before received date.");

            if (productItem.Expiry_Date < stockOutDto.SoldDate)
                throw new ArgumentException("Cannot sell an expired product.");


            // Update item status to Sold 
            productItem.Status = ProductItemStatus.Sold;

            // Create stock out record
            var stockOut = new StockOut
            {
                ProductItemId = stockOutDto.ProductItemId,
                UserId = userId,
                SoldDate = stockOutDto.SoldDate
            };

            await _context.StockOuts.AddAsync(stockOut);

            // Transaction Record
            var transaction = new Transaction
            {
                ProductItemId = stockOutDto.ProductItemId,
                UserId = userId,
                TransactionType = TransactionType.StockOut,
                TransactionDate = stockOutDto.SoldDate
            };

            await _context.Transactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            await _productService.UpdateProductStatusAsync(productItem.ProductId);

            return Ok(new { success = true, message = "Stock out recorded successfully." });
        }

        [HttpPost("scan-out")]
        public async Task<IActionResult> ScanAndStockOut(QrStockOutDto dto)
        {
            var userId = GetValidUserId(); 

            // Parse QR code format
            var parts = dto.QRCode.Split('|');
            if (parts.Length != 6 || parts[0] != "PIID" || parts[2] != "SN" || parts[4] != "PID")
                throw new ArgumentException("Invalid QR code format.");

            if (!int.TryParse(parts[1], out int productItemId) || !int.TryParse(parts[5], out int productId))
                throw new ArgumentException("Invalid product item ID or product ID.");

            var serialNumber = parts[3].Trim();

            // Find matching product item
            var productItem = await _context.ProductItems
                .FirstOrDefaultAsync(pi =>
                    pi.Id == productItemId &&
                    pi.ProductId == productId &&
                    pi.Serial_Number == serialNumber);

            if (productItem == null)
                throw new KeyNotFoundException("Product item not found.");

            if (productItem.Status != ProductItemStatus.InStock)
                throw new ArgumentException("Product item is not in stock.");

            if (dto.SoldDate > DateTime.UtcNow)
                throw new ArgumentException("Sold date cannot be in the future.");

            var alreadySold = await _context.StockOuts
                .AnyAsync(so => so.ProductItemId == productItem.Id);

            if (alreadySold)
                throw new ArgumentException("This item is already sold.");

            var stockIn = await _context.StockIns
                .FirstOrDefaultAsync(si => si.ProductItemId == productItem.Id);

            if (stockIn == null)
                throw new InvalidOperationException("This item hasn't been stocked in yet.");

            if (dto.SoldDate < stockIn.ReceivedDate)
                throw new ArgumentException("Sold date cannot be before received date.");

            if (productItem.Expiry_Date < dto.SoldDate)
                throw new ArgumentException("Cannot sell an expired product.");

            // Update item status to Sold
            productItem.Status = ProductItemStatus.Sold;

            // Add stock out record
            var stockOut = new StockOut
            {
                ProductItemId = productItem.Id,
                UserId = userId,
                SoldDate = dto.SoldDate
            };
            await _context.StockOuts.AddAsync(stockOut);

            // Log transaction
            var transaction = new Transaction
            {
                ProductItemId = productItem.Id,
                UserId = userId,
                TransactionType = TransactionType.StockOut,
                TransactionDate = dto.SoldDate
            };
            await _context.Transactions.AddAsync(transaction);

            // Save changes and update product status
            await _context.SaveChangesAsync();
            await _productService.UpdateProductStatusAsync(productItem.ProductId);

            return Ok(new
            {
                success = true,
                message = "Product item stocked out successfully via QR scan.",
                ProductItemId = productItem.Id,
                productItem.Status
            });
        }
    }
}
