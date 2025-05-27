using System.ComponentModel.DataAnnotations;

namespace JwtAuth.Models
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public required string Name { get; set; } 
        public required string Description { get; set; }
        public Guid UserId { get; set; }
        public List<ProductDto> Products { get; set; } = new List<ProductDto>();
    }
}
