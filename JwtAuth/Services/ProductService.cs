using JwtAuth.Data;
using JwtAuth.Entity.Enums;
using Microsoft.EntityFrameworkCore;

namespace JwtAuth.Services
{
    public class ProductService
    {
        private readonly AppDbContext context;

        public ProductService(AppDbContext context)
        {
            this.context = context;
        }

        public void UpdateProductStatus(int productId)
        {
            var product = context.Products
                .Include(p => p.ProductItems)
                .FirstOrDefault(p => p.Id == productId);

            if (product == null || product.Status == ProductStatus.Discontinued)
                return;

            int inStock = product.ProductItems.Count(pi => pi.Status == ProductItemStatus.InStock);
            product.Status = inStock == 0 ? ProductStatus.OutOfStock : ProductStatus.Available;

            context.SaveChanges();
        }
    }

}
