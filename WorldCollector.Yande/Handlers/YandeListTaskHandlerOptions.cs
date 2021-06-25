using System.Collections.Generic;
using TaskQueue.CommonTaskQueues.Handlers.CrawlerTaskHandler;

namespace WorldCollector.Yande.Handlers
{
    public class YandeListTaskHandlerOptions : CrawlerTaskHandlerOptions
    {
        public string ListUrlTpl { get; set; }
    }
}
