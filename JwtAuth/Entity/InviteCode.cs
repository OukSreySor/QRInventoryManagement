namespace JwtAuth.Entity
{
    public class InviteCode : BaseEntity
    {
        public int Id { get; set; }
        public string Code { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
        public bool IsUsed { get; set; } = false;
        public DateTime? UsedAt { get; set; }
    }
}
