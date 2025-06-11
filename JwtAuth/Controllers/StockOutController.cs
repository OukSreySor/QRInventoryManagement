using JwtAuth.Data;
using JwtAuth.Entity;
using JwtAuth.Entity.Enums;
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
    }
}
