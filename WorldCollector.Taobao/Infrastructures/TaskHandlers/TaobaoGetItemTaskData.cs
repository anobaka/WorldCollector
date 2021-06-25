
using TaskQueue;

namespace WorldCollector.Taobao.Infrastructures.TaskHandlers
{
    public class TaobaoGetItemTaskData : TaskData
    {
        public string ItemId { get; set; }
    }
}
