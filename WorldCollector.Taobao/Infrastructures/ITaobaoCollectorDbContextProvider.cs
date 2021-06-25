using System.Threading.Tasks;
using WorldCollector.Taobao.Infrastructures.Models;

namespace WorldCollector.Taobao.Infrastructures
{
    public interface ITaobaoCollectorDbContextProvider
    {
        Task<TaobaoCollectorDbContext> Get(bool cache = false);
    }
}
