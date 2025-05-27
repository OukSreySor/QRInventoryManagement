namespace JwtAuth.Entity
{
    public class Transaction
    {
        public int Id { get; set; }
        public int ProductItemId { get; set; }
        public ProductItem ProductItem { get; set; } = null!;
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string TransactionType { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
    }
}
