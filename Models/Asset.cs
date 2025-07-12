using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MarketAssetsApi.Models
{
    [Index(nameof(Symbol), nameof(Provider), IsUnique = true)]
    public class Asset
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Symbol { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; }

        [MaxLength(100)]
        public string InstrumentId { get; set; }

        [MaxLength(50)]
        public string Provider { get; set; }
    }
} 