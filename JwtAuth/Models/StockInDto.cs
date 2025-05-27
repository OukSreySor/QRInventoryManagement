namespace JwtAuth.Models
{
    public class StockInDto
    {
        public required int ProductItemId { get; set; }
        public Guid UserId { get; set; }
        public required DateTime ReceivedDate { get; set; }
    } 
}
