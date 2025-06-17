using JwtAuth.Data;
using JwtAuth.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace JwtAuth.Services
{
    public class ProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task UpdateProductStatusAsync(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductItems)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || product.Status == ProductStatus.Discontinued)
                return;

            int inStock = product.ProductItems.Count(pi => pi.Status == ProductItemStatus.InStock);
            product.Status = inStock == 0 ? ProductStatus.OutOfStock : ProductStatus.Available;

            await _context.SaveChangesAsync();
        }
    }

}
