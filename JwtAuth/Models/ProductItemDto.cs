using System.ComponentModel.DataAnnotations;

namespace JwtAuth.Models
{
    public class ProductItemDto
    {
        public int Id { get; set; }
        public string? QR_Code { get; set; }
        public required string Serial_Number { get; set; } 
        public required string Status { get; set; }
        public required int ProductId { get; set; }
        public DateTime Manufacturing_Date { get; set; }
        public DateTime Expiry_Date { get; set; }
        public Guid UserId { get; set; }
    }
}
