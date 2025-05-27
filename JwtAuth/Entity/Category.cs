namespace JwtAuth.Entity
{
    public class Category: BaseEntity
    {
        public int Id { get; set; } 
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty ;
        public ICollection<Product> Products { get; set; } 
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
