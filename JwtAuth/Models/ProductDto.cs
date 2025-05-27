using System.ComponentModel.DataAnnotations;

namespace JwtAuth.Models
{
    public class ProductDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public string? Image { get; set; }    
        public required decimal Unit_Price { get; set; }
        public required decimal Selling_Price { get; set; }
        public int Quantity { get; set; }
        public required int CategoryId { get; set; }
        public Guid UserId { get; set; }
        public ICollection<ProductItemDto> ProductItems { get; set; } = new List<ProductItemDto>();

    }
}
