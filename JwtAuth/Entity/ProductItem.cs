using JwtAuth.Entity.Enums;

namespace JwtAuth.Entity
{
    public class ProductItem: BaseEntity
    {
        public int Id { get; set; }
        public string? QR_Code { get; set; } 
        public string Serial_Number { get; set; } = string.Empty;
        public ProductItemStatus Status { get; set; } 
        public DateTime Manufacturing_Date { get; set; }
        public DateTime Expiry_Date { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<StockIn>? StockIns { get; set; }
        public ICollection<StockOut>? StockOuts { get; set; }


    }
}
