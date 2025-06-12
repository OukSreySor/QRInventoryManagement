using System.Text;
using JwtAuth.Data;
using JwtAuth.Entity;
using JwtAuth.Entity.Enums;
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
        private readonly AppDbContext context;
        private readonly ProductService productService;
        public StockOutController(AppDbContext context, ProductService productService)
        {
            this.context = context;
            this.productService = productService;
        }

        [HttpPost("stock-out")]
        public IActionResult StockOut(StockOutDto stockOutDto)
        {
            var userId = GetValidUserId(); 
            var userRole = GetValidUserRole();

            var productItem = context.ProductItems.FirstOrDefault(pi => pi.Id == stockOutDto.ProductItemId);
            if (productItem == null)
                return NotFound($"ProductItem with Id {stockOutDto.ProductItemId} not found.");

            if (userRole == "User" && productItem.UserId != userId)
            {
                return Forbid("You are not allowed to stock out this product item.");
            }

            if (productItem.Status != ProductItemStatus.InStock)
                return BadRequest("ProductItem is not currently in stock.");

            // Update item status to Sold 
            productItem.Status = ProductItemStatus.Sold;

            // Create stock out record
            var stockOut = new StockOut
            {
                ProductItemId = stockOutDto.ProductItemId,
                UserId = userId,
                SoldDate = stockOutDto.SoldDate
            };

            context.StockOuts.Add(stockOut);

            // Transaction Record
            var transaction = new Transaction
            {
                ProductItemId = stockOutDto.ProductItemId,
                UserId = userId,
                TransactionType = TransactionType.StockOut,
                TransactionDate = stockOutDto.SoldDate
            };

            context.Transactions.Add(transaction);
            context.SaveChanges();
            productService.UpdateProductStatus(productItem.ProductId);

            return Ok(new { message = "Stock out recorded successfully." });
        }

        [HttpPost("scan-out")]
        public IActionResult ScanAndStockOut(QrStockOutDto dto)
        {
            var userId = GetValidUserId(); 
            var userRole = GetValidUserRole();

            // Parse QR code format
            var parts = dto.QRCode.Split('-');
            if (parts.Length != 6 || parts[0] != "PIID" || parts[2] != "SN" || parts[4] != "PID")
                return BadRequest("Invalid QR code format.");

            if (!int.TryParse(parts[1], out int productItemId) || !int.TryParse(parts[5], out int productId))
                return BadRequest("Invalid product item ID or product ID.");

            var serialNumber = parts[3];

            // Find matching product item
            var productItem = context.ProductItems
                .FirstOrDefault(pi =>
                    pi.Id == productItemId &&
                    pi.ProductId == productId &&
                    pi.Serial_Number == serialNumber);

            if (productItem == null)
                return NotFound("Product item not found.");

            // Validate access
            if (userRole == "User" && productItem.UserId != userId)
                return Forbid("You are not allowed to stock out this product item.");

            if (productItem.Status != ProductItemStatus.InStock)
                return BadRequest("Product item is not in stock.");

            // Update item status to Sold
            productItem.Status = ProductItemStatus.Sold;

            // Add stock out record
            var stockOut = new StockOut
            {
                ProductItemId = productItem.Id,
                UserId = userId,
                SoldDate = dto.SoldDate
            };
            context.StockOuts.Add(stockOut);

            // Log transaction
            var transaction = new Transaction
            {
                ProductItemId = productItem.Id,
                UserId = userId,
                TransactionType = TransactionType.StockOut,
                TransactionDate = dto.SoldDate
            };
            context.Transactions.Add(transaction);

            // Save changes and update product status
            context.SaveChanges();
            productService.UpdateProductStatus(productItem.ProductId);

            return Ok(new
            {
                message = "Product item stocked out successfully via QR scan.",
                ProductItemId = productItem.Id,
                Status = productItem.Status
            });
        }

        [HttpGet("export-csv")]
        public IActionResult ExportTransactionsToCsv(DateTime? startDate, DateTime? endDate, string? username)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            // Reuse same queries from GetTransactions...
            var transactions = GetFilteredTransactions(startDate, endDate, username, userId, userRole);

            var csv = new StringBuilder();
            csv.AppendLine("ProductItemId,QRCode,SerialNumber,Status,TransactionType,TransactionDate,Username");

            foreach (var t in transactions)
            {
                csv.AppendLine($"{t.ProductItemId},{t.QRCode},{t.SerialNumber},{t.Status},{t.TransactionType},{t.TransactionDate:yyyy-MM-dd},{t.Username}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "transactions_report.csv");
        }

        private List<TransactionDto> GetFilteredTransactions(DateTime? startDate, DateTime? endDate, string? username, Guid userId, string userRole)
        {
            var stockInsQuery = context.StockIns.Include(si => si.ProductItem).Include(si => si.User).AsQueryable();
            var stockOutsQuery = context.StockOuts.Include(so => so.ProductItem).Include(so => so.User).AsQueryable();

            if (userRole == "User")
            {
                stockInsQuery = stockInsQuery.Where(si => si.UserId == userId);
                stockOutsQuery = stockOutsQuery.Where(so => so.UserId == userId);
            }

            if (startDate.HasValue)
            {
                stockInsQuery = stockInsQuery.Where(si => si.ReceivedDate >= startDate);
                stockOutsQuery = stockOutsQuery.Where(so => so.SoldDate >= startDate);
            }

            if (endDate.HasValue)
            {
                stockInsQuery = stockInsQuery.Where(si => si.ReceivedDate <= endDate);
                stockOutsQuery = stockOutsQuery.Where(so => so.SoldDate <= endDate);
            }

            if (!string.IsNullOrEmpty(username) && userRole == "Admin")
            {
                stockInsQuery = stockInsQuery.Where(si => si.User.Username.Contains(username));
                stockOutsQuery = stockOutsQuery.Where(so => so.User.Username.Contains(username));
            }

            var stockIns = stockInsQuery.Select(si => new TransactionDto
            {
                ProductItemId = si.ProductItemId,
                QRCode = si.ProductItem.QR_Code,
                SerialNumber = si.ProductItem.Serial_Number,
                Status = si.ProductItem.Status,
                TransactionType = TransactionType.StockIn,
                TransactionDate = si.ReceivedDate,
                Username = si.User.Username
            });

            var stockOuts = stockOutsQuery.Select(so => new TransactionDto
            {
                ProductItemId = so.ProductItemId,
                QRCode = so.ProductItem.QR_Code,
                SerialNumber = so.ProductItem.Serial_Number,
                Status = so.ProductItem.Status,
                TransactionType = TransactionType.StockOut,
                TransactionDate = so.SoldDate,
                Username = so.User.Username
            });

            return stockIns.Union(stockOuts).OrderByDescending(t => t.TransactionDate).ToList();
        }


    }
}
