using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CsQuery;
using TaskQueue.CommonTaskQueues.Handlers.CrawlerTaskHandler;

namespace WorldCollector.Taobao.Collectors.ItemImageCollector.TaskHandlers
{
    public class TaobaoGetItemImageUrlListTaskHandlerOptions : CrawlerTaskHandlerOptions
    {
        public string UrlTemplate { get; set; }
//        public Func<string, CQ, Task<List<DownloadTaskData>>> GetImageTaskDataFunc { get; set; }
//        public Func<TaobaoCollectorDbContext> CrawlRecordDbFunc { get; set; }
    }
}
