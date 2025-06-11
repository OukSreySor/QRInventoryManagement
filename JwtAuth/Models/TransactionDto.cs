using System.Text.Json.Serialization;
using JwtAuth.Entity.Enums;

namespace JwtAuth.Models
{
    namespace JwtAuth.Models
    {
        public class TransactionDto
        {
            public required int ProductItemId { get; set; }
            public string? QRCode { get; set; }
            public required string SerialNumber { get; set; }

            [JsonConverter(typeof(JsonStringEnumConverter))]
            public required ProductItemStatus Status { get; set; }

            [JsonConverter(typeof(JsonStringEnumConverter))]
            public required TransactionType TransactionType { get; set; } 
            public required DateTime TransactionDate { get; set; } 
            public required string Username { get; set; } 
        }
    }

}
