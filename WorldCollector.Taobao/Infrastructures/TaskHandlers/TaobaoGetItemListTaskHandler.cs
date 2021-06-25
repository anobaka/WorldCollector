using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CsQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProxyProvider.HttpClientProvider;
using TaskQueue;
using TaskQueue.CommonTaskQueues.Handlers.CrawlerTaskHandler;

namespace WorldCollector.Taobao.Infrastructures.TaskHandlers
{
    public class
        TaobaoGetItemListTaskHandler : CrawlerTaskHandler<TaobaoGetItemListTaskHandlerOptions,
            TaobaoGetItemListTaskData>
    {
        protected readonly ITaobaoCollectorDbContextProvider DbContextProvider;

        public TaobaoGetItemListTaskHandler(IOptions<TaobaoGetItemListTaskHandlerOptions> options,
            ITaskDistributor taskDistributor, IHttpClientProvider httpClientProvider,
            ITaobaoCollectorDbContextProvider dbContextProvider = null) : base(options, taskDistributor,
            httpClientProvider)
        {
            DbContextProvider = dbContextProvider;
        }

        protected override async Task HandleInternalUnstatable(TaobaoGetItemListTaskData taskData,
            CancellationToken ct)
        {
            var client = await GetHttpClient();
            var url = string.Format(Options.Value.ListUrlTpl, taskData.Page);
            var rsp = await client.GetAsync(url, ct);
            var html = rsp.StatusCode == HttpStatusCode.Redirect
                ? await client.GetStringAsync(rsp.Headers.Location)
                : await rsp.Content.ReadAsStringAsync();
            var cq = new CQ(html.Replace("\\\"", "\""));
            var searchResultSpan = cq[".search-result span"];
            var count = int.Parse(searchResultSpan.Text().Trim());
            if (count > 0)
            {
                var itemIds = cq[".shop-filter"].NextAll().Children(".item").Select(t => t.GetAttribute("data-id"))
                    .ToList();
                if (itemIds.Any())
                {
                    var newTaskData = new List<TaskData> {new TaobaoGetItemListTaskData {Page = taskData.Page + 1}};
                    if (DbContextProvider != null)
                    {
                        var db = await DbContextProvider.Get();
                        var soonestCheckDt = DateTime.Now.AddDays(-7);
                        var skippedItems = await db.TaobaoItems
                            .Where(t => itemIds.Contains(t.ItemId) && t.LastCheckDt > soonestCheckDt)
                            .Select(a => a.ItemId).ToListAsync(ct);
                        itemIds.RemoveAll(t => skippedItems.Contains(t));
                    }

                    if (itemIds.Any())
                    {
                        newTaskData.AddRange(itemIds
                            .Select(t => new TaobaoGetItemTaskData {ItemId = t}).ToList());
                    }

                    await TaskDistributor.Distribute(newTaskData);
                }
            }
        }
    }
}