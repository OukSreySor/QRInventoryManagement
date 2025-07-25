using JwtAuth.Entity.Enums;
using System.Text.Json.Serialization;

namespace JwtAuth.Models
{
    public class CreateProductDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required decimal Unit_Price { get; set; }
        public required decimal Selling_Price { get; set; }
        public int Quantity { get; set; } 

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductStatus Status { get; set; }

        public required int CategoryId { get; set; }
        public Guid UserId { get; set; }
        public ICollection<ProductItemDto> ProductItems { get; set; } = new List<ProductItemDto>();

        
    }
}
