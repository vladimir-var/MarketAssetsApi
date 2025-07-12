using Microsoft.EntityFrameworkCore;

namespace MarketAssetsApi.Models
{
    public class MarketAssetsDbContext : DbContext
    {
        public MarketAssetsDbContext(DbContextOptions<MarketAssetsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Asset> Assets { get; set; }
        public DbSet<Price> Prices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(entity.GetTableName().ToLower());
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(property.GetColumnName().ToLower());
                }
                foreach (var key in entity.GetKeys())
                {
                    key.SetName(key.GetName().ToLower());
                }
                foreach (var index in entity.GetIndexes())
                {
                    index.SetDatabaseName(index.GetDatabaseName().ToLower());
                }
            }
        }
    }
} 