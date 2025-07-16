namespace JwtAuth.Models
{
    public class ProductItemStockInDto
    {
        public required string Serial_Number { get; set; }
        public DateTime Manufacturing_Date { get; set; }
        public DateTime Expiry_Date { get; set; }
        public DateTime AddedDate { get; set; }
        public required int ProductId { get; set; }
    }
}
