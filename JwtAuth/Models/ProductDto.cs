﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JwtAuth.Entity.Enums;

namespace JwtAuth.Models
{
    public class ProductDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public string? Image { get; set; }    
        public required decimal Unit_Price { get; set; }
        public required decimal Selling_Price { get; set; }
        public int Quantity { get; set; } // Computed from ProductItems count InStock

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductStatus Status { get; set; }

        public required int CategoryId { get; set; }
        public Guid UserId { get; set; }
        public ICollection<ProductItemDto> ProductItems { get; set; } = new List<ProductItemDto>();

        public CategoryDto Category { get; set; }


    }
}
