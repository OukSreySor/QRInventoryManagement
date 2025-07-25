namespace JwtAuth.Models
{
    public class ProductStockSummaryDto
    {
        public int Id { get; set; }
        public required string Name { get; set; } 
        public required string CategoryName { get; set; }
        public int QuantityInStock { get; set; }
    }
}
