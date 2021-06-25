using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskQueue;

namespace WorldCollector.Taobao.Collectors.ItemImageCollector
{
    public class TaobaoItemImageCollectorOptions : TaskQueuePoolOptions
    {
        public string ShopUrl { get; set; }
        public string Name { get; set; }
        public int ListThreads { get; set; }
        public int ListInterval { get; set; }
        public int ItemThreads { get; set; }
        public int ItemInterval { get; set; }
        public int DownloadThreads { get; set; }
        public int DownloadInterval { get; set; }

        public List<string> ExcludeImageUrls { get; set; }
        public string DownloadPath { get; set; }
        public string HttpClientProviderDbConnectionString { get; set; }
        public string CrawlRecordDbConnectionString { get; set; }
    }
}