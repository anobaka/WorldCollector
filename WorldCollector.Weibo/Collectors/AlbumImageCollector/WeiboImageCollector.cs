using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProxyProvider.HttpClientProvider;
using TaskQueue;
using TaskQueue.CommonTaskQueues.Handlers.DownloadTaskHandler;
using TaskQueue.CommonTaskQueues.Pools.StatableTaskQueuePool;
using TaskQueue.CommonTaskQueues.Queues.StatableTaskQueue;
using WorldCollector.Weibo.Collectors.AlbumImageCollector.TaskHandlers;

namespace WorldCollector.Weibo.Collectors.AlbumImageCollector
{
    public class WeiboImageCollector : StatableTaskQueuePool<WeiboImageCollectorOptions>
    {
        private const string ProxyPurpose = "WeiboImage";

        public WeiboImageCollector(IOptions<WeiboImageCollectorOptions> options, ILoggerFactory loggerFactory) : base(
            options, loggerFactory)
        {
        }

        public override async Task Start()
        {
            Add(new StatableTaskQueue<WeiboPhotoListTaskHandler>(Microsoft.Extensions.Options.Options.Create(
                new TaskQueueOptions
                {
                    MaxThreads = Options.Value.ListThreads,
                    Interval = Options.Value.ListInterval
                }), new WeiboPhotoListTaskHandler(Microsoft.Extensions.Options.Options.Create(
                    new WeiboPhotoListTaskHandlerOptions
                    {
                        HttpClientPurpose = ProxyPurpose, UrlTemplate = Options.Value.ListUrlTemplate
                    }), this,
                new ConfigurableHttpClientProvider(Options.Value.HttpClientProviderDbConnectionString,
                    new ProxyProvider.ProxyProvider(Options.Value.HttpClientProviderDbConnectionString)))));

            Add(new StatableTaskQueue<DownloadTaskHandler>(Microsoft.Extensions.Options.Options.Create(
                new TaskQueueOptions
                {
                    Interval = Options.Value.ListInterval,
                    MaxThreads = Options.Value.ListThreads
                }), new DownloadTaskHandler(Microsoft.Extensions.Options.Options.Create(
                new DownloadTaskHandlerOptions
                {
                    DownloadPath = Options.Value.DownloadPath,
                }), this, null, null), this));

            await Distribute(new WeiboPhotoListTaskData {Page = 1});
            await base.Start();
        }
    }
}