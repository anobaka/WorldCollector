using TaskQueue;

namespace WorldCollector.Taobao.Infrastructures.TaskHandlers
{
    public class TaobaoGetItemListTaskData : TaskData
    {
        public int Page { get; set; }
    }
}
