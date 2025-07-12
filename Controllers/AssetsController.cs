using Microsoft.AspNetCore.Mvc;
using MarketAssetsApi.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MarketAssetsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetsController : ControllerBase
    {
        private readonly MarketAssetsDbContext _context;

        public AssetsController(MarketAssetsDbContext context)
        {
            _context = context;
        }

        // GET: api/assets
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAssets()
        {
            return await _context.Assets.ToListAsync();
        }
    }
} 