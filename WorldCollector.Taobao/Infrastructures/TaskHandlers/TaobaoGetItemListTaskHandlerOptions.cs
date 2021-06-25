using TaskQueue.CommonTaskQueues.Handlers.CrawlerTaskHandler;

namespace WorldCollector.Taobao.Infrastructures.TaskHandlers
{
    public class TaobaoGetItemListTaskHandlerOptions : CrawlerTaskHandlerOptions
    {
        public string ListUrlTpl { get; set; }
    }
}