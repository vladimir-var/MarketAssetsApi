using Microsoft.AspNetCore.Mvc;
using MarketAssetsApi.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System;

namespace MarketAssetsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PricesController : ControllerBase
    {
        private readonly MarketAssetsDbContext _context;

        public PricesController(MarketAssetsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPrices([FromQuery] string assets)
        {
            if (string.IsNullOrWhiteSpace(assets))
                return BadRequest("Parameter 'assets' is required (comma-separated symbols)");

            var symbols = assets.Split(',').Select(s => s.Trim()).ToList();

            var prices = await _context.Prices
                .Include(p => p.Asset)
                .Where(p => symbols.Contains(p.Asset.Symbol))
                .GroupBy(p => p.Asset.Symbol)
                .Select(g => g.OrderByDescending(p => p.UpdatedAt).First())
                .ToListAsync();

            var result = prices.Select(p => new
            {
                Asset = p.Asset.Symbol,
                Price = p.Value,
                UpdatedAt = p.UpdatedAt
            });

            return Ok(result);
        }
    }
} 