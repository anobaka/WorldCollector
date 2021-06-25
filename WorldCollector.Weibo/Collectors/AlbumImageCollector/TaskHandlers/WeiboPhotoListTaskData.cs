
using TaskQueue;

namespace WorldCollector.Weibo.Collectors.AlbumImageCollector.TaskHandlers
{
    public class WeiboPhotoListTaskData : TaskData
    {
        public string SinceId { get; set; }
        public int Page { get; set; }
    }
}
