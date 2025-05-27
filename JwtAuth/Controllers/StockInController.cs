using JwtAuth.Data;
using JwtAuth.Entity;
using JwtAuth.Models;
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
        private readonly AppDbContext context;
        private readonly IWebHostEnvironment env;

        public StockInController(AppDbContext context, IWebHostEnvironment env)
        {
            this.context = context;
            this.env = env;
        }

        [HttpPost("stock-in")]
        public IActionResult StockIn(StockInDto stockInDto)
        {
            var userId = GetValidUserId();

            var productItem = context.ProductItems.FirstOrDefault(pi => pi.Id == stockInDto.ProductItemId);
            if (productItem == null)
                return NotFound($"ProductItem with Id {stockInDto.ProductItemId} not found.");
            
            if (productItem.Status == "InStock")
                return BadRequest("Product item already stocked in.");

            // Update item status to InStock
            productItem.Status = "InStock";

            // Create stock in record
            var stockIn = new StockIn
            {
                ProductItemId = stockInDto.ProductItemId,
                UserId = userId,
                ReceivedDate = stockInDto.ReceivedDate
            };

            context.StockIns.Add(stockIn);

            // Transaction Record
            var transaction = new Transaction
            {
                ProductItemId = stockInDto.ProductItemId,
                UserId = userId,
                TransactionType = "StockIn",
                TransactionDate = stockInDto.ReceivedDate
            };

            context.Transactions.Add(transaction);
            context.SaveChanges();

            return Ok(new 
            { 
                message = "Stock in recorded successfully."
            });
        }

    }
}
