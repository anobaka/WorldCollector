using System;
using System.Collections.Generic;
using System.Text;
using TaskQueue;

namespace WorldCollector.Yande
{
    public class YandeCollectorOptions : TaskQueuePoolOptions
    {
        public string Site { get; set; }
        public string DownloadPath { get; set; } = "./Collection/";
        public string ListUrlTemplate { get; set; }
        public int ListInterval { get; set; }
        public int ListThreads { get; set; }
        public int DownloadInterval { get; set; }
        public int DownloadThreads { get; set; }
    }
}