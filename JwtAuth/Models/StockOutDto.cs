namespace JwtAuth.Models
{
    public class StockOutDto
    {
        public required int ProductItemId { get; set; }
        public Guid UserId { get; set; }
        public required DateTime SoldDate { get; set; }
    }
}
