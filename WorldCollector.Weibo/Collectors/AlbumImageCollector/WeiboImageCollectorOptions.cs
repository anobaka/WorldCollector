using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using TaskQueue;

namespace WorldCollector.Weibo.Collectors.AlbumImageCollector
{
    public class WeiboImageCollectorOptions : TaskQueuePoolOptions
    {
        [Key]
        public string Name { get; set; }
        public string ListUrlTemplate { get; set; }
        public int ListInterval { get; set; }
        public int ListThreads { get; set; }
        public int DownloadInterval { get; set; }
        public int DownloadThreads { get; set; }

        [Required]
        public string DownloadPath { get; set; }

        public string HttpClientProviderDbConnectionString { get; set; }
    }
}
