using Microsoft.EntityFrameworkCore;
using WorldCollector.Taobao.Infrastructures.Models;

namespace WorldCollector.Taobao.Infrastructures
{
    public class TaobaoCollectorDbContext : DbContext
    {
        public DbSet<TaobaoItem> TaobaoItems { get; set; }

        public TaobaoCollectorDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaobaoItem>(t =>
            {
                t.HasIndex(a => new {Id = a.ItemId, a.LastCheckDt});
            });
            base.OnModelCreating(modelBuilder);
        }
    }
}
