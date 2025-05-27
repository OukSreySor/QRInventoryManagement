using System.ComponentModel;
using JwtAuth.Models;

namespace JwtAuth.Entity
{
    public class Product: BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Image { get; set; }
        public decimal Unit_Price { get; set; }
        public decimal Selling_Price { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<ProductItem> ProductItems { get; set; } 

    }


}
