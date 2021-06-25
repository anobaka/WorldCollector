using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CsQuery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProxyProvider;
using ProxyProvider.Abstractions;
using ProxyProvider.HttpClientProvider;
using TaskQueue;
using TaskQueue.CommonTaskQueues.Handlers.DownloadTaskHandler;
using TaskQueue.CommonTaskQueues.Pools.StatableTaskQueuePool;
using TaskQueue.CommonTaskQueues.Queues.StatableTaskQueue;
using WorldCollector.Taobao.Collectors.ItemImageCollector.TaskHandlers;
using WorldCollector.Taobao.Infrastructures;
using WorldCollector.Taobao.Infrastructures.TaskHandlers;

namespace WorldCollector.Taobao.Collectors.ItemImageCollector
{
    public abstract class TaobaoItemImageCollector : StatableTaskQueuePool<TaobaoItemImageCollectorOptions>
    {
        private const string SearchUri = "/search.htm";
        private const string AsyncSearchUriElementId = "J_ShopAsynSearchURL";
        private const string PageParameterName = "pageNo";
        private const string ProxyPurpose = "Taobao";

        private const string ItemUrlTemplate = "https://item.taobao.com/item.htm?id={0}";
        private readonly IHttpClientProvider _httpClientProvider;
        private readonly IProxyProvider _proxyProvider;
        private readonly ITaobaoGetItemImageUrlListExtractor _taobaoGetItemImageUrlListExtractor;
        private readonly IDownloadTaskFilter _downloadTaskFilter;

        protected TaobaoItemImageCollector(IOptions<TaobaoItemImageCollectorOptions> options,
            ILoggerFactory loggerFactory, IProxyProvider proxyProvider,
            ITaobaoGetItemImageUrlListExtractor taobaoGetItemImageUrlListExtractor,
            IDownloadTaskFilter downloadTaskFilter) :
            base(options, loggerFactory)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _httpClientProvider =
                new ConfigurableHttpClientProvider(options.Value.HttpClientProviderDbConnectionString, _proxyProvider);
            _proxyProvider = proxyProvider;
            _taobaoGetItemImageUrlListExtractor = taobaoGetItemImageUrlListExtractor;
            _downloadTaskFilter = downloadTaskFilter;
        }

        /// <summary>
        /// By shopUrl.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task<string> GetListUrlTemplate()
        {
            var client = await _httpClientProvider.GetClient(ProxyPurpose);
            var shopUri = new Uri(Options.Value.ShopUrl);
            var searchAllUrl = new Uri(shopUri, SearchUri);
            var rsp = await client.GetAsync(searchAllUrl);
            var html = rsp.StatusCode == HttpStatusCode.Redirect
                ? await client.GetStringAsync(rsp.Headers.Location)
                : await rsp.Content.ReadAsStringAsync();
            var asyncSearchUri = new CQ(html)[$"#{AsyncSearchUriElementId}"].Val();
            if (string.IsNullOrEmpty(asyncSearchUri))
            {
                throw new ArgumentNullException(nameof(asyncSearchUri));
            }

            var asyncSearchUrl = new Uri(shopUri, asyncSearchUri).ToString();
            asyncSearchUrl += (asyncSearchUrl.Contains("?") ? "&" : "?") + $"{PageParameterName}={{0}}";
            return asyncSearchUrl;
        }

        public override async Task Start()
        {
            if (!string.IsNullOrEmpty(Options.Value.CrawlRecordDbConnectionString))
            {
                TaobaoCollectorDbContext GetRecordDbFunc() => new TaobaoCollectorDbContext(
                    new DbContextOptionsBuilder<TaobaoCollectorDbContext>()
                        .UseSqlServer(Options.Value.CrawlRecordDbConnectionString).Options);

                await GetRecordDbFunc().Database.MigrateAsync();
            }

            var listUrlTemplate = await GetListUrlTemplate();
            Add(new StatableTaskQueue<TaobaoGetItemListTaskHandler>(Microsoft.Extensions.Options.Options.Create(
                new TaskQueueOptions
                {
                    Interval = Options.Value.ListInterval,
                    MaxThreads = Options.Value.ListThreads
                }), new TaobaoGetItemListTaskHandler(Microsoft.Extensions.Options.Options.Create(
                new TaobaoGetItemListTaskHandlerOptions
                {
                    ListUrlTpl = listUrlTemplate
                }), this, _httpClientProvider), this));

            Add(new StatableTaskQueue<TaobaoGetItemImageUrlListTaskHandler>(Microsoft.Extensions.Options.Options.Create(
                new TaskQueueOptions
                {
                    Interval = Options.Value.ListInterval,
                    MaxThreads = Options.Value.ListThreads
                }), new TaobaoGetItemImageUrlListTaskHandler(Microsoft.Extensions.Options.Options.Create(
                new TaobaoGetItemImageUrlListTaskHandlerOptions
                {
                    UrlTemplate = ""
                }), this, _httpClientProvider, _taobaoGetItemImageUrlListExtractor), this));

            Add(new StatableTaskQueue<DownloadTaskHandler>(Microsoft.Extensions.Options.Options.Create(
                new TaskQueueOptions
                {
                    Interval = Options.Value.ListInterval,
                    MaxThreads = Options.Value.ListThreads
                }), new DownloadTaskHandler(Microsoft.Extensions.Options.Options.Create(
                new DownloadTaskHandlerOptions
                {
                    DownloadPath = Options.Value.DownloadPath,
                }), this, _httpClientProvider, _downloadTaskFilter), this));

            await Distribute(new TaobaoGetItemListTaskData {Page = 1});
            await base.Start();
        }
    }
}