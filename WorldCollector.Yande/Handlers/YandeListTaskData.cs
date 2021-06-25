using System.Collections.Generic;
using Newtonsoft.Json;
using TaskQueue;

namespace WorldCollector.Yande.Handlers
{
    public class YandeListTaskData : TaskData
    {
        public int Page { get; set; }
        [JsonIgnore] public List<int> LastImageIds { get; set; }
        [JsonIgnore] public List<int> CrawledImageIds { get; set; } = new List<int>();
    }
}