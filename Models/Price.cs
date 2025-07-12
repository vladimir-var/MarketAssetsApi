using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketAssetsApi.Models
{
    public class Price
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [Column("asset_id")]
        public Guid AssetId { get; set; }

        [ForeignKey("AssetId")]
        public Asset Asset { get; set; }

        [Required]
        [Column(TypeName = "numeric(18,6)")]
        public decimal Value { get; set; }

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 