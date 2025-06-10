using JwtAuth.Data;
using JwtAuth.Entity;
using JwtAuth.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace JwtAuth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin, User")]
    public class ProductItemController : BaseController
    {
        private readonly AppDbContext context;
        private readonly IWebHostEnvironment env;

        public ProductItemController(AppDbContext context, IWebHostEnvironment env)
        {
            this.context = context;
            this.env = env;
        }

        [HttpGet]
        public IActionResult GetProductItems()
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var productItemsQuery = context.ProductItems.AsQueryable();

            if (userRole == "User")
            {
                productItemsQuery = productItemsQuery.Where(pi => pi.UserId == userId);
            }

            var productItems = productItemsQuery
                .Select(pi => new ProductItemDto
                {
                    Id = pi.Id,
                    QR_Code = pi.QR_Code,
                    Serial_Number = pi.Serial_Number,
                    Status = pi.Status,
                    ProductId = pi.ProductId,
                    UserId = pi.UserId
                }).ToList();
            return Ok(productItems);
        }

        [HttpGet("{id}")]
        public IActionResult GetProductItem(int id)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var productItemQuery = context.ProductItems.Where(pi => pi.Id == id);

            if (userRole == "User")
            {
                productItemQuery = productItemQuery.Where(pi => pi.UserId == userId);
            }
            var productItem = productItemQuery
                .Select(pi => new ProductItemDto
                {
                    Id = pi.Id,
                    QR_Code = pi.QR_Code,
                    Serial_Number = pi.Serial_Number,
                    Status = pi.Status,
                    ProductId = pi.ProductId,
                    UserId = pi.UserId
                }).FirstOrDefault();

            if (productItem == null) return NotFound();

            return Ok(productItem);
        }

        [HttpPost]
        public IActionResult CreateProductItem(ProductItemDto productItemDto)
        {
            var userId = GetValidUserId();
            var productExists = context.Products.Any(p => p.Id == productItemDto.ProductId && p.UserId == userId);
            if (!productExists)
            {
                return BadRequest("Invalid ProductId. Product does not exist.");
            }

            var productItem = new ProductItem
            {
                Serial_Number = productItemDto.Serial_Number,
                Status = productItemDto.Status,
                Manufacturing_Date = productItemDto.Manufacturing_Date,
                Expiry_Date = productItemDto.Expiry_Date,
                ProductId = productItemDto.ProductId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.ProductItems.Add(productItem);
            context.SaveChanges();

            // Generate QR string (unique text identifier)
            var qrString = $"PIID-{productItem.Id}-SN-{productItem.Serial_Number}-PID-{productItem.ProductId}";

            // Save QR string to product item
            productItem.QR_Code = qrString;
            context.SaveChanges();

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(15); 

            var qrFolder = Path.Combine(env.WebRootPath ?? "wwwroot", "qrcodes");
            if (!Directory.Exists(qrFolder))
                Directory.CreateDirectory(qrFolder);

            var fileName = $"{qrString}.png";
            var filePath = Path.Combine(qrFolder, fileName);

            using (var image = Image.Load<Rgba32>(qrBytes))
            {
                image.Save(filePath, new PngEncoder());
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var qrImageUrl = $"{baseUrl}/qrcodes/{fileName}";

            var result = new
            {
                productItem.Id,
                productItem.Serial_Number,
                productItem.Status,
                productItem.ProductId,
                productItem.Manufacturing_Date,
                productItem.Expiry_Date,
                productItem.QR_Code,
                QRImageUrl = qrImageUrl
            };

            return Ok(result);

        }
        [HttpGet("scan")]
        public IActionResult ScanProductItem(string code)
        {
            var userId = GetValidUserId();

            // Format: PIID-1-SN-ABC-PID-1
            var parts = code.Split('-');

            if (parts.Length != 6 || parts[0] != "PIID" || parts[2] != "SN" || parts[4] != "PID")
                return BadRequest("Invalid QR code format.");

            if (!int.TryParse(parts[1], out int productItemId) || !int.TryParse(parts[5], out int productId))
                return BadRequest("Invalid ID values in QR code.");

            var serialNumber = parts[3];

            var productItem = context.ProductItems
                .Include(pi => pi.Product) 
                .FirstOrDefault(pi =>
                    pi.Id == productItemId &&
                    pi.Serial_Number == serialNumber &&
                    pi.ProductId == productId &&
                    pi.Product.UserId == userId
                );

            if (productItem == null)
                return NotFound("Product item not found.");

            var result = new
            {
                ProductItemId = productItem.Id,
                productItem.Serial_Number,
                productItem.Status,
                productItem.Manufacturing_Date,
                productItem.Expiry_Date,
                productItem.QR_Code,
                Product = new
                {
                    productItem.Product.Id,
                    productItem.Product.Name,
                    productItem.Product.Description,
                    productItem.Product.Unit_Price,
                    productItem.Product.Selling_Price
                }
            };

            return Ok(result);
        }
        [HttpPut("{id}")]
        public IActionResult EditProductItem(int id, ProductItemDto productItemDto)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var productItemQuery = context.ProductItems.Where(pi => pi.Id == id);

            if (userRole == "User")
            {
                productItemQuery = productItemQuery.Where(pi => pi.UserId == userId);
            }

            var productItem = productItemQuery.FirstOrDefault();
            if (productItem == null)
            {
                return NotFound();
            }

            var productExists = context.Products.Any(p => p.Id == productItemDto.ProductId &&
                                              (userRole == "Admin" || p.UserId == userId));
            if (!productExists)
            {
                return BadRequest("Invalid ProductId or access denied.");
            }

            productItem.Serial_Number = productItemDto.Serial_Number;
            productItem.Status = productItemDto.Status;
            productItem.Manufacturing_Date = productItemDto.Manufacturing_Date;
            productItem.Expiry_Date = productItemDto.Expiry_Date;
            productItem.ProductId = productItemDto.ProductId;
            productItem.UpdatedAt = DateTime.UtcNow;

            context.SaveChanges();

            return Ok(productItem);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProductItem(int id)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var productItemQuery = context.ProductItems.Where(pi => pi.Id == id);

            if (userRole == "User")
            {
                productItemQuery = productItemQuery.Where(pi => pi.UserId == userId);
            }

            var item = productItemQuery.FirstOrDefault();
            if (item == null)
            {
                return NotFound("ProductItem not found.");
            }

            context.ProductItems.Remove(item);
            context.SaveChanges();

            return Ok("ProductItem delete success.");
        }
    }
}
