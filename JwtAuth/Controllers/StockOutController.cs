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
    public class StockOutController : BaseController
    {
        private readonly AppDbContext context;
        public StockOutController(AppDbContext context)
        {
            this.context = context;
        }

        [HttpPost("stock-out")]
        public IActionResult StockOut(StockOutDto stockOutDto)
        {
            var productItem = context.ProductItems.FirstOrDefault(pi => pi.Id == stockOutDto.ProductItemId);
            if (productItem == null)
                return NotFound($"ProductItem with Id {stockOutDto.ProductItemId} not found.");

            if (productItem.Status != "InStock")
                return BadRequest("ProductItem is not currently in stock.");

            // Update item status to Sold 
            productItem.Status = "Sold";

            var userId = GetValidUserId();

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
                TransactionType = "StockOut",
                TransactionDate = stockOutDto.SoldDate
            };

            context.Transactions.Add(transaction);
            context.SaveChanges();

            return Ok(new { message = "Stock out recorded successfully." });
        }
    }
}
