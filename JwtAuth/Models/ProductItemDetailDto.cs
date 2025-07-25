namespace JwtAuth.Models
{
    public class ProductItemDetailDto
    {
        public int Id { get; set; }
        public required string Serial_Number { get; set; }
        public required DateTime Manufacturing_Date { get; set; }
        public required DateTime Expiry_Date { get; set; }
        public required decimal Unit_Price { get; set; }
        public required decimal Selling_Price { get; set; }
        public required string QR_Code { get; set; }
        public required string QRImageUrl { get; set; }
        public required string ProductName { get; set; }
        public required string UserName { get; set; }
        public required DateTime AddedDate { get; set; }
    }

}
