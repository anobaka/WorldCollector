using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace WorldCollector.Taobao.Infrastructures
{
    public class DefaultTaobaoCollectorDbContextProvider : ITaobaoCollectorDbContextProvider
    {
        private readonly string _dbConnectionString;
        private TaobaoCollectorDbContext _cache;

        public DefaultTaobaoCollectorDbContextProvider(string dbConnectionString)
        {
            _dbConnectionString = dbConnectionString;
        }

        public Task<TaobaoCollectorDbContext> Get(bool cache = false)
        {
            TaobaoCollectorDbContext ctx;
            if (!cache || _cache == null)
            {
                ctx = new TaobaoCollectorDbContext(new DbContextOptionsBuilder<TaobaoCollectorDbContext>()
                    .UseSqlServer(_dbConnectionString).Options);
                if (_cache == null)
                {
                    _cache = ctx;
                }
            }
            else
            {
                ctx = _cache;
            }

            return Task.FromResult(ctx);
        }
    }
}