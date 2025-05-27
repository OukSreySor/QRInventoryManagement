namespace JwtAuth.Entity
{
    public class StockOut: BaseEntity
    {
        public int Id { get; set; }
        public int ProductItemId { get; set; }
        public ProductItem ProductItem { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime SoldDate { get; set; }
    }
}
