using TaskQueue.CommonTaskQueues.Handlers.CrawlerTaskHandler;

namespace WorldCollector.Weibo.Collectors.AlbumImageCollector.TaskHandlers
{
    public class WeiboPhotoListTaskHandlerOptions : CrawlerTaskHandlerOptions
    {
        public string UrlTemplate { get; set; }
    }
}
