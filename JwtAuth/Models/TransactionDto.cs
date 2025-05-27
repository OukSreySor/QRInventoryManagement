namespace JwtAuth.Models
{
    namespace JwtAuth.Models
    {
        public class TransactionDto
        {
            public required int ProductItemId { get; set; }
            public string? QRCode { get; set; }
            public required string SerialNumber { get; set; }
            public required string Status { get; set; }
            public required string TransactionType { get; set; } // "StockIn" or "StockOut"
            public required DateTime TransactionDate { get; set; } 
            public required string Username { get; set; } 
        }
    }

}
