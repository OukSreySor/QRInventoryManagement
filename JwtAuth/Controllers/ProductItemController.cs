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
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ProductService _productService;

        public ProductItemController(AppDbContext context, IWebHostEnvironment env, ProductService productService)
        {
            _context = context;
            _env = env;
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProductItems()
        {
            var productItems = await _context.ProductItems
                .Select(pi => new ProductItemDto
                {
                    Id = pi.Id,
                    QR_Code = pi.QR_Code,
                    Serial_Number = pi.Serial_Number,
                    Status = pi.Status,
                    ProductId = pi.ProductId,
                    UserId = pi.UserId
                }).ToListAsync();

            return Ok(new { success = true, data = productItems });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductItem(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid product item ID.");

            var productItem = await _context.ProductItems
                .Where(pi => pi.Id == id)
                .Select(pi => new ProductItemDto
                {
                    Id = pi.Id,
                    QR_Code = pi.QR_Code,
                    Serial_Number = pi.Serial_Number,
                    Status = pi.Status,
                    ProductId = pi.ProductId,
                    UserId = pi.UserId
                }).FirstOrDefaultAsync();

            if (productItem == null)
                throw new KeyNotFoundException("Product item not found.");

            return Ok(new { success = true, data = productItem });
        }

        [HttpPost]
        public async Task<IActionResult> CreateProductItem(ProductItemDto productItemDto)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            bool productExists;
            if (userRole == "User")
            {
                productExists = await _context.Products
                    .AnyAsync(p => p.Id == productItemDto.ProductId && p.UserId == userId);
            }
            else if (userRole == "Admin")
            {
                productExists = await _context.Products
                    .AnyAsync(p => p.Id == productItemDto.ProductId);
            }
            else
            {
                throw new UnauthorizedAccessException("Invalid user role.");
            }

            if (!productExists)
                throw new ArgumentException("Invalid ProductId or access denied.");
            
            var isDuplicateSerial = await _context.ProductItems
                 .AnyAsync(p => p.Serial_Number == productItemDto.Serial_Number &&
                                p.ProductId == productItemDto.ProductId);

            if (isDuplicateSerial)
                throw new ArgumentException("Duplicate serial number within the same product.");

            if (productItemDto.Manufacturing_Date > productItemDto.Expiry_Date)
                throw new ArgumentException("Expiry date must be after manufacturing date.");

            if (productItemDto.Expiry_Date <= DateTime.UtcNow)
                throw new ArgumentException("Expiry date must be a future date.");

            if (productItemDto.Manufacturing_Date > DateTime.UtcNow)
                throw new ArgumentException("Manufacturing date cannot be in the future.");

            var productItem = new ProductItem
            {
                Serial_Number = productItemDto.Serial_Number,
                Status = ProductItemStatus.PendingStockIn,
                Manufacturing_Date = productItemDto.Manufacturing_Date,
                Expiry_Date = productItemDto.Expiry_Date,
                ProductId = productItemDto.ProductId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.ProductItems.AddAsync(productItem);
            await _context.SaveChangesAsync();
            await _productService.UpdateProductStatusAsync(productItem.ProductId);

            // Generate QR string (unique text identifier)
            var qrString = $"PIID|{productItem.Id}|SN|{productItem.Serial_Number}|PID|{productItem.ProductId}";

            // Save QR string to product item
            productItem.QR_Code = qrString;
            await _context.SaveChangesAsync();

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(15); 

            var qrFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "qrcodes");
            if (!Directory.Exists(qrFolder))
                Directory.CreateDirectory(qrFolder);

            var safeFileName = qrString.Replace("|", "_");
            var fileName = $"{safeFileName}.png";
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

            return Ok(new { success = true, data = result });

        }
        [HttpGet("scan")]
        public async Task<IActionResult> ScanProductItem(string code)
        {
            var userId = GetValidUserId();

            // Format: PIID|1|SN|ABC|PID|1
            var parts = code.Split('|');

            if (parts.Length != 6 || parts[0] != "PIID" || parts[2] != "SN" || parts[4] != "PID")
                throw new ArgumentException("Invalid QR code format.");

            if (!int.TryParse(parts[1], out int productItemId) || !int.TryParse(parts[5], out int productId))
                throw new ArgumentException("Invalid ID values in QR code.");

            var serialNumber = parts[3];

            var productItem = await _context.ProductItems
                .Include(pi => pi.Product) 
                .FirstOrDefaultAsync(pi =>
                    pi.Id == productItemId &&
                    pi.Serial_Number == serialNumber &&
                    pi.ProductId == productId
                );

            if (productItem == null)
                throw new KeyNotFoundException("Product item not found.");

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

            return Ok(new { success = true, data = result });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> EditProductItem(int id, ProductItemDto productItemDto)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            if (id <= 0)
                throw new ArgumentException("Invalid product item ID.");

            var productItemQuery = _context.ProductItems.Where(pi => pi.Id == id);

            if (userRole == "User")
            {
                productItemQuery = productItemQuery.Where(pi => pi.UserId == userId);
            }

            var productItem = await productItemQuery.FirstOrDefaultAsync();
            if (productItem == null)
                throw new KeyNotFoundException("Product item not found.");

            var productExists = await _context.Products.AnyAsync(p => p.Id == productItemDto.ProductId &&
                                              (userRole == "Admin" || p.UserId == userId));
            if (!productExists)
                throw new ArgumentException("Invalid ProductId or access denied.");

            if (productItemDto.ProductId <= 0)
                throw new ArgumentException("Product ID must be a positive number.");

            var isDuplicateSerial = await _context.ProductItems.AnyAsync(p =>
                p.Id != id &&
                p.Serial_Number == productItemDto.Serial_Number &&
                p.ProductId == productItemDto.ProductId);
            if (isDuplicateSerial)
                throw new ArgumentException("Duplicate serial number within the same product.");

            if (productItemDto.Manufacturing_Date > productItemDto.Expiry_Date)
                throw new ArgumentException("Expiry date must be after manufacturing date.");

            if (productItemDto.Expiry_Date <= DateTime.UtcNow)
                throw new ArgumentException("Expiry date must be a future date.");

            if (productItemDto.Manufacturing_Date > DateTime.UtcNow)
                throw new ArgumentException("Manufacturing date cannot be in the future.");

            productItem.Serial_Number = productItemDto.Serial_Number;
            productItem.Manufacturing_Date = productItemDto.Manufacturing_Date;
            productItem.Expiry_Date = productItemDto.Expiry_Date;
            productItem.ProductId = productItemDto.ProductId;
            productItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = productItem });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductItem(int id)
        {
            var userId = GetValidUserId();
            var userRole = GetValidUserRole();

            var productItemQuery = _context.ProductItems.Where(pi => pi.Id == id);

            if (userRole == "User")
            {
                productItemQuery = productItemQuery.Where(pi => pi.UserId == userId);
            }

            var item = await productItemQuery.FirstOrDefaultAsync();
            if (item == null)
                throw new KeyNotFoundException("Product item not found.");

            _context.ProductItems.Remove(item);
            await _context.SaveChangesAsync();
            await _productService.UpdateProductStatusAsync(item.ProductId);

            return Ok(new { success = true, message = "Product item deleted successfully." });
        }

        [HttpPost("create-stockin")]
        public async Task<IActionResult> CreateAndStockIn(ProductItemStockInDto dto)
        {
            var userId = GetValidUserId();
            
            var productExists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId);
            if (!productExists)
                throw new ArgumentException("Invalid ProductId.");

            if (dto.Manufacturing_Date > dto.Expiry_Date)
                throw new ArgumentException("Expiry date must be after manufacturing date.");

            if (dto.Expiry_Date <= DateTime.UtcNow)
                throw new ArgumentException("Expiry date must be a future date.");

            if (dto.Manufacturing_Date > DateTime.UtcNow)
                throw new ArgumentException("Manufacturing date cannot be in the future.");

            if (dto.AddedDate > DateTime.UtcNow)
                throw new ArgumentException("Added date cannot be in the future.");

            if (dto.AddedDate < dto.Manufacturing_Date)
                throw new ArgumentException("Added date cannot be before manufacturing date.");

            if (dto.AddedDate > dto.Expiry_Date)
                throw new ArgumentException("Added date cannot be after expiry date.");

            var isDuplicateSerial = await _context.ProductItems
                .AnyAsync(p => p.Serial_Number == dto.Serial_Number && p.ProductId == dto.ProductId);

            if (isDuplicateSerial)
                throw new ArgumentException("Duplicate serial number within the same product.");

            // Create product item with status InStock
            var productItem = new ProductItem
            {
                Serial_Number = dto.Serial_Number,
                Status = ProductItemStatus.InStock,             // This is a stock-in operation, so mark it as InStock
                Manufacturing_Date = dto.Manufacturing_Date,
                Expiry_Date = dto.Expiry_Date,
                ProductId = dto.ProductId,
                UserId = userId, 
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.ProductItems.AddAsync(productItem);
            await _context.SaveChangesAsync();

            var qrString = $"PIID|{productItem.Id}|SN|{productItem.Serial_Number}|PID|{productItem.ProductId}";
            productItem.QR_Code = qrString;
            await _context.SaveChangesAsync();

            // Generate QR image
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(15);

            var qrFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "qrcodes");
            if (!Directory.Exists(qrFolder))
                Directory.CreateDirectory(qrFolder);

            var safeFileName = qrString.Replace("|", "_");
            var fileName = $"{safeFileName}.png";
            var filePath = Path.Combine(qrFolder, fileName);

            using (var image = Image.Load<Rgba32>(qrBytes))
            {
                image.Save(filePath, new PngEncoder());
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var qrImageUrl = $"{baseUrl}/qrcodes/{fileName}";

            // Create StockIn record
            var stockIn = new StockIn
            {
                ProductItemId = productItem.Id,
                UserId = userId,
                ReceivedDate = dto.AddedDate
            };
            await _context.StockIns.AddAsync(stockIn);

            // Create Transaction record
            var transaction = new Transaction
            {
                ProductItemId = productItem.Id,
                UserId = userId,
                TransactionType = TransactionType.StockIn,
                TransactionDate = dto.AddedDate
            };
            await _context.Transactions.AddAsync(transaction);

            await _context.SaveChangesAsync();
            await _productService.UpdateProductStatusAsync(productItem.ProductId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    productItem.Id,
                    productItem.Serial_Number,
                    productItem.Status,
                    productItem.ProductId,
                    productItem.Manufacturing_Date,
                    productItem.Expiry_Date,
                    productItem.QR_Code,
                    QRImageUrl = qrImageUrl
                }
            });
        }

        [HttpGet("by-product/{productId}")]
        public async Task<IActionResult> GetProductItemsByProduct(int productId)
        {
            var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
            if (!productExists)
            {
                return NotFound(new { success = false, message = "Product not found." });
            }

            var query = _context.ProductItems
                .Include(pi => pi.Product)
                .Include(pi => pi.User)
                .Include(pi => pi.StockIns) 
                .Where(pi => pi.ProductId == productId);

            var items = await query.Select(pi => new ProductItemDetailDto
            {
                Id = pi.Id,
                Serial_Number = pi.Serial_Number,
                Manufacturing_Date = pi.Manufacturing_Date,
                Expiry_Date = pi.Expiry_Date,
                Unit_Price = pi.Product.Unit_Price,
                Selling_Price = pi.Product.Selling_Price,
                QR_Code = pi.QR_Code,
                QRImageUrl = $"{Request.Scheme}://{Request.Host}/qrcodes/PIID_{pi.Id}_SN_{pi.Serial_Number}_PID_{pi.ProductId}.png",
                ProductName = pi.Product.Name,
                UserName = pi.User != null ? pi.User.Username : "Unknown",
                AddedDate = pi.StockIns.Any() 
                        ? pi.StockIns.OrderByDescending(s => s.ReceivedDate).First().ReceivedDate
                        : pi.CreatedAt
            }).ToListAsync();

            return Ok(new { success = true, data = items });
        }




    }
}
