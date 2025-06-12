using JwtAuth.Data;
using JwtAuth.Entity.Enums;
using JwtAuth.Models.JwtAuth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JwtAuth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin, User")]
    public class TransactionController : BaseController
    {
        private readonly AppDbContext context;
        public TransactionController(AppDbContext context)
        {
            this.context = context;
        }

        [HttpGet("transactions")]
        public IActionResult GetTransactions(DateTime? startDate, DateTime? endDate, string? username)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var stockInsQuery = context.StockIns
                .Include(si => si.ProductItem)
                .Include(si => si.User)
                .AsQueryable();

            var stockOutsQuery = context.StockOuts
                .Include(so => so.ProductItem)
                .Include(so => so.User)
                .AsQueryable();

            // User role Limitation
            if (userRole == "User")
            {
                stockInsQuery = stockInsQuery.Where(si => si.UserId == userId);
                stockOutsQuery = stockOutsQuery.Where(so => so.UserId == userId);
            }
            // Filter by date range
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
            // Filter by username (admin only)
            if (!string.IsNullOrEmpty(username) && userRole == "Admin")
            {
                stockInsQuery = stockInsQuery.Where(si => si.User.Username.Contains(username));
                stockOutsQuery = stockOutsQuery.Where(so => so.User.Username.Contains(username));
            }

            var stockIns = stockInsQuery
                .Select(si => new TransactionDto
                {
                    ProductItemId = si.ProductItemId,
                    QRCode = si.ProductItem.QR_Code,
                    SerialNumber = si.ProductItem.Serial_Number,
                    Status = si.ProductItem.Status,
                    TransactionType = TransactionType.StockIn,
                    TransactionDate = si.ReceivedDate,
                    Username = si.User.Username
                });

            var stockOuts = stockOutsQuery
                .Select(so => new TransactionDto
                {
                    ProductItemId = so.ProductItemId,
                    QRCode = so.ProductItem.QR_Code,
                    SerialNumber = so.ProductItem.Serial_Number,
                    Status = so.ProductItem.Status,
                    TransactionType = TransactionType.StockOut,
                    TransactionDate = so.SoldDate,
                    Username = so.User.Username
                });

            var allTransactions = stockIns
                .Union(stockOuts)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();

            return Ok(allTransactions);
        }

        [HttpGet("summary")]
        public IActionResult GetSummary(DateTime? startDate, DateTime? endDate)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            // Limit to user's items if role is User
            var productItemsQuery = context.ProductItems.AsQueryable();
            if (userRole == "User")
            {
                productItemsQuery = productItemsQuery.Where(pi => pi.UserId == userId);
            }

            // Count products by status
            var totalInStock = productItemsQuery.Count(pi => pi.Status == ProductItemStatus.InStock);
            var totalSold = productItemsQuery.Count(pi => pi.Status == ProductItemStatus.Sold);

            // Base queries for transactions
            var stockInsQuery = context.StockIns.AsQueryable();
            var stockOutsQuery = context.StockOuts.AsQueryable();

            if (userRole == "User")
            {
                stockInsQuery = stockInsQuery.Where(si => si.UserId == userId);
                stockOutsQuery = stockOutsQuery.Where(so => so.UserId == userId);
            }

            if (startDate.HasValue)
            {
                stockInsQuery = stockInsQuery.Where(si => si.ReceivedDate >= startDate.Value);
                stockOutsQuery = stockOutsQuery.Where(so => so.SoldDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                stockInsQuery = stockInsQuery.Where(si => si.ReceivedDate <= endDate.Value);
                stockOutsQuery = stockOutsQuery.Where(so => so.SoldDate <= endDate.Value);
            }

            var totalStockIns = stockInsQuery.Count();
            var totalStockOuts = stockOutsQuery.Count();

            // Recent activity - last 7 days transactions count
            var recentStockIns = stockInsQuery
                .Where(si => si.ReceivedDate >= DateTime.UtcNow.AddDays(-7))
                .Count();

            var recentStockOuts = stockOutsQuery
                .Where(so => so.SoldDate >= DateTime.UtcNow.AddDays(-7))
                .Count();

            return Ok(new
            {
                TotalInStock = totalInStock,
                TotalSold = totalSold,
                TotalStockIns = totalStockIns,
                TotalStockOuts = totalStockOuts,
                RecentStockInsLast7Days = recentStockIns,
                RecentStockOutsLast7Days = recentStockOuts
            });
        }


    }
}
