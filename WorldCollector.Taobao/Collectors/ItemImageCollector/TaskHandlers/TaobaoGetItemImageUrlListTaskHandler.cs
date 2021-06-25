using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CsQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProxyProvider.HttpClientProvider;
using TaskQueue;
using TaskQueue.CommonTaskQueues.Handlers.CrawlerTaskHandler;
using TaskQueue.CommonTaskQueues.Handlers.DownloadTaskHandler;
using WorldCollector.Taobao.Infrastructures;
using WorldCollector.Taobao.Infrastructures.Models;
using WorldCollector.Taobao.Infrastructures.TaskHandlers;

namespace WorldCollector.Taobao.Collectors.ItemImageCollector.TaskHandlers
{
    public class
        TaobaoGetItemImageUrlListTaskHandler : CrawlerTaskHandler<TaobaoGetItemImageUrlListTaskHandlerOptions,
            TaobaoGetItemTaskData>
    {
        protected ITaobaoGetItemImageUrlListExtractor ImageUrlListExtractor;
        protected readonly ITaobaoCollectorDbContextProvider DbContextProvider;

        public TaobaoGetItemImageUrlListTaskHandler(IOptions<TaobaoGetItemImageUrlListTaskHandlerOptions> options,
            ITaskDistributor taskDistributor, IHttpClientProvider httpClientProvider,
            ITaobaoGetItemImageUrlListExtractor imageUrlListExtractor,
            ITaobaoCollectorDbContextProvider dbContextProvider = null)
            : base(options, taskDistributor,
                httpClientProvider)
        {
            ImageUrlListExtractor = imageUrlListExtractor;
            DbContextProvider = dbContextProvider;
        }

        protected override async Task HandleInternalUnstatable(TaobaoGetItemTaskData taskData,
            CancellationToken ct)
        {
            var client = await GetHttpClient();
            var url = string.Format(Options.Value.UrlTemplate, taskData.ItemId);
            var html = await client.GetStringAsync(url);
            var cq = new CQ(html);
            var title = cq["#J_Title h3"].Attr("data-title").Trim();
            //china url
            var match = Regex.Match(html, @"descUrl\s*\:\slocation.*(?<url>\/\/.*?)',").Groups["url"];
            //world url
            if (!match.Success)
            {
                match = Regex.Match(html, "descUrlSSL\\s*\\:\\s*\"(?<url>\\/\\/.*?)\".*").Groups["url"];
            }

            var descUrl = match.Value;
            if (descUrl.StartsWith("//"))
            {
                descUrl = $"https:{descUrl}";
            }

            var descJsonp = await client.GetStringAsync(descUrl);
            var descHtml = Regex.Match(descJsonp, @"var\s*desc\s*=\s*'\s*(?<html>[\s\S]*)\s*'\s*;")
                .Groups["html"].Value;
            var descCq = new CQ(descHtml);
            var urlList = await ImageUrlListExtractor.ExtractImageUrlList(descCq);
            var index = 0;
            var newTaskData = urlList.Select(t =>
            {
                var filename = $"{index++}_{t.Substring(t.LastIndexOf('/') + 1)}";
                if (filename.Contains("?"))
                {
                    filename = filename.Substring(0, filename.IndexOf('?'));
                }

                var data = new DownloadTaskData
                {
                    RelativeFilename = $"{title}/{filename}",
                    Url = t
                };
                return data;
            }).ToList();

            if (DbContextProvider != null)
            {
                var db = await DbContextProvider.Get();
                var record = await db.TaobaoItems.FirstOrDefaultAsync(t => t.ItemId == taskData.ItemId, ct);
                if (record == null)
                {
                    record = new TaobaoItem
                    {
                        ItemId = taskData.ItemId
                    };
                    db.Add(record);
                }

                record.LastCheckDt = DateTime.Now;
                await db.SaveChangesAsync(ct);
            }

            await TaskDistributor.Distribute(newTaskData);
        }
    }
}