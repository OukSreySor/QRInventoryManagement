using JwtAuth.Data;
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
        public IActionResult GetTransactions()
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

            if (userRole == "User")
            {
                stockInsQuery = stockInsQuery.Where(si => si.UserId == userId);
                stockOutsQuery = stockOutsQuery.Where(so => so.UserId == userId);
            }
            var stockIns = stockInsQuery
                .Select(si => new TransactionDto
                {
                    ProductItemId = si.ProductItemId,
                    QRCode = si.ProductItem.QR_Code,
                    SerialNumber = si.ProductItem.Serial_Number,
                    Status = si.ProductItem.Status,
                    TransactionType = "StockIn",
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
                    TransactionType = "StockOut",
                    TransactionDate = so.SoldDate,
                    Username = so.User.Username
                });

            var allTransactions = stockIns
                .Union(stockOuts)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();

            return Ok(allTransactions);
        }

    }
}
