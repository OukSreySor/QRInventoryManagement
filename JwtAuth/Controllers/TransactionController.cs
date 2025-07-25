using JwtAuth.Data;
using JwtAuth.Entity;
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
        private readonly AppDbContext _context;
        public TransactionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(DateTime? startDate, DateTime? endDate, string? username)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var stockInsQuery = _context.StockIns
                .Include(si => si.ProductItem)
                .Include(si => si.User)
                .AsQueryable();

            var stockOutsQuery = _context.StockOuts
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

            var allTransactions = await stockIns
                .Union(stockOuts)
                .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

            return Ok(new { success = true, data = allTransactions });
        }

        [HttpGet("activity-log")]
        public async Task<IActionResult> GetActivityLog()
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var query = _context.Transactions
                .Include(t => t.ProductItem)
                .ThenInclude(pi => pi.Product)
                .Include(t => t.User)
                .AsQueryable();

            if (userRole == "User")
            {
                query = query.Where(t => t.UserId == userId);
            }
            
            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new
                {
                    ItemId = t.ProductItem.Id,
                    ItemName = t.ProductItem.Product.Name,
                    QRCode = t.ProductItem.QR_Code,
                    SerialNumber = t.ProductItem.Serial_Number,
                    TransactionType = t.TransactionType.ToString(),
                    t.TransactionDate,
                    UserName = t.User.Username,
                    TransactionValue = t.TransactionType == TransactionType.StockOut
                                   ? t.ProductItem.Product.Selling_Price
                                   : t.ProductItem.Product.Unit_Price
                })
                .ToListAsync();

            return Ok(new { 
                success = true, 
                data = transactions,
                
            });
        }

        [HttpGet("counts")]
        public async Task<IActionResult> GetTransactionCounts()
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var today = DateTime.UtcNow.Date;

            IQueryable<Transaction> query = _context.Transactions
                .Include(t => t.ProductItem)
                .ThenInclude(pi => pi.Product)
                .Include(t => t.User);

            if (userRole == "User")
            {
                // Filter only user's transactions for today
                query = query.Where(t => t.UserId == userId && t.TransactionDate.Date == today);
            }
            else
            {
                // For Admin, get all today's transactions
                query = query.Where(t => t.TransactionDate.Date == today);
            }

            var stockInCount = await query.CountAsync(t => t.TransactionType == TransactionType.StockIn);
            var stockOutCount = await query.CountAsync(t => t.TransactionType == TransactionType.StockOut);

            return Ok(new
            {
                success = true,
                StockInCount = stockInCount,
                StockOutCount = stockOutCount
            });
        }


        [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivityLog(int limit = 5) 
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var query = _context.Transactions
                .Include(t => t.ProductItem)
                    .ThenInclude(pi => pi.Product)
                .Include(t => t.User) 
                .AsQueryable();

            if (userRole == "User")
            {
                query = query.Where(t => t.UserId == userId);
            }

            var recentTransactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Take(limit) 
                .Select(t => new
                {
                    ProductName = t.ProductItem.Product.Name,
                    SerialNumber = t.ProductItem.Serial_Number,
                    TransactionType = t.TransactionType.ToString(),
                    t.TransactionDate,
                    //TransactionDate = DateTime.SpecifyKind(t.TransactionDate, DateTimeKind.Local),
                    UserName = t.User.Username 
                })
                .ToListAsync();
            foreach (var t in recentTransactions)
            {
                Console.WriteLine($"TransactionDate raw: {t.TransactionDate}, Kind: {t.TransactionDate.Kind}");
            }


            return Ok(new { success = true, data = recentTransactions });
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(DateTime? startDate, DateTime? endDate)
        {
            var userId = GetValidUserId();
            
            var productItemsQuery = _context.ProductItems.AsQueryable();

            // Apply date filtering on product items based on creation or updated date
            if (startDate.HasValue)
            {
                productItemsQuery = productItemsQuery.Where(pi => pi.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                productItemsQuery = productItemsQuery.Where(pi => pi.CreatedAt <= endDate.Value);
            }

            // Count products by status (with date filter)
            var totalInStock = await productItemsQuery.CountAsync(pi => pi.Status == ProductItemStatus.InStock);
            var totalSold = await productItemsQuery.CountAsync(pi => pi.Status == ProductItemStatus.Sold);

            // Base queries for transactions (stock ins and stock outs)
            var stockInsQuery = _context.StockIns.AsQueryable();
            var stockOutsQuery = _context.StockOuts.AsQueryable();

            // Date filtering on stock in/out queries
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

            var totalStockIns = await stockInsQuery.CountAsync();
            var totalStockOuts = await stockOutsQuery.CountAsync();

            var netStock = totalStockIns - totalStockOuts;

            // Recent activity - last 7 days transactions count
            var recentStockIns = await stockInsQuery
                .Where(si => si.ReceivedDate >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();

            var recentStockOuts = await stockOutsQuery
                .Where(so => so.SoldDate >= DateTime.UtcNow.AddDays(-7))
                .CountAsync();

            // Get products (include price info)
            var products = await _context.Products.ToListAsync();
            var productMap = products.ToDictionary(p => p.Id, p => p);

            // Filtered product items - use the date-filtered productItemsQuery from above
            var productItems = await productItemsQuery.ToListAsync();

            // Group product items by product
            var groupedItems = productItems.GroupBy(pi => pi.ProductId);

            decimal inventoryValue = 0;
            decimal totalSales = 0;
            decimal potentialProfit = 0;

            foreach (var group in groupedItems)
            {
                var productId = group.Key;
                if (!productMap.ContainsKey(productId)) continue;

                var product = productMap[productId];
                var items = group.ToList();

                var inStockCount = items.Count(i => i.Status == ProductItemStatus.InStock);
                var soldCount = items.Count(i => i.Status == ProductItemStatus.Sold);

                inventoryValue += inStockCount * product.Unit_Price;
                totalSales += soldCount * product.Selling_Price;

                var profitPerItem = product.Selling_Price - product.Unit_Price;
                potentialProfit += soldCount * profitPerItem;
            }

            // Category breakdown
            var categoryBreakdown = await _context.Categories
                .Select(c => new
                {
                    CategoryName = c.Name,
                    ProductCount = c.Products.Count(),
                    TotalUnits = c.Products.SelectMany(p => p.ProductItems).Count()
                }).ToListAsync();

            int lowStockThreshold = 3;
            var lowStockCount = groupedItems.Count(group =>
            {
                var inStockCount = group.Count(pi => pi.Status == ProductItemStatus.InStock);
                return inStockCount <= lowStockThreshold;
            });

            // Detailed low stock products list
            var lowStockProducts = groupedItems
                .Where(group =>
                {
                    var inStockCount = group.Count(pi => pi.Status == ProductItemStatus.InStock);
                    return inStockCount <= lowStockThreshold;
                })
                .Select(group =>
                {
                    var product = productMap[group.Key];
                    var inStockCount = group.Count(pi => pi.Status == ProductItemStatus.InStock);
                    return new
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        InStockUnits = inStockCount
                    };
                }).ToList();

            // Hot products last 30 days with date filtering already applied to stockOutsQuery
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var stockOutsLast30Days = await stockOutsQuery
                .Where(so => so.SoldDate >= thirtyDaysAgo)
                .Include(so => so.ProductItem)
                .ThenInclude(pi => pi.Product)
                .ToListAsync();

            var hotProducts30Days = stockOutsLast30Days
                .GroupBy(so => so.ProductItem.ProductId)
                .Select(g =>
                {
                    var product = productMap[g.Key];
                    var unitsSold = g.Count();
                    var totalSalesValue = unitsSold * product.Selling_Price;
                    return new
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitsSold = unitsSold,
                        TotalSales = totalSalesValue
                    };
                })
                .OrderByDescending(x => x.UnitsSold)
                .Take(3)
                .ToList();

            // Stock trends for last 7 days
            var stockTrendStartDate = DateTime.UtcNow.Date.AddDays(-6); // last 7 days
            var trendDates = Enumerable.Range(0, 7).Select(i => stockTrendStartDate.AddDays(i)).ToList();

            var stockInsPerDay = await stockInsQuery
                .Where(si => si.ReceivedDate >= stockTrendStartDate)
                .GroupBy(si => si.ReceivedDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var stockOutsPerDay = await stockOutsQuery
                .Where(so => so.SoldDate >= stockTrendStartDate)
                .GroupBy(so => so.SoldDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var stockTrends = trendDates.Select(date =>
            {
                var stockIn = stockInsPerDay.FirstOrDefault(d => d.Date == date)?.Count ?? 0;
                var stockOut = stockOutsPerDay.FirstOrDefault(d => d.Date == date)?.Count ?? 0;
                return new
                {
                    Date = date.ToString("MM-dd"),
                    StockIn = stockIn,
                    StockOut = stockOut
                };
            }).ToList();

            return Ok(new
            {
                success = true,
                TotalInStock = totalInStock,
                TotalSold = totalSold,
                TotalStockIns = totalStockIns,
                TotalStockOuts = totalStockOuts,
                RecentStockInsLast7Days = recentStockIns,
                RecentStockOutsLast7Days = recentStockOuts,
                NetStock = netStock,
                InventoryValue = inventoryValue,
                TotalSalesValue = totalSales,
                PotentialProfit = potentialProfit,
                CategoryBreakdown = categoryBreakdown,
                LowStockCount = lowStockCount,
                LowStockProducts = lowStockProducts,
                HotProducts30Days = hotProducts30Days,
                StockTrends = stockTrends
            });
        }


    }
}
