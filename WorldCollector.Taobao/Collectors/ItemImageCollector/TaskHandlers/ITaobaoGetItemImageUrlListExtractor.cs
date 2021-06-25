using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CsQuery;

namespace WorldCollector.Taobao.Collectors.ItemImageCollector.TaskHandlers
{
    public interface ITaobaoGetItemImageUrlListExtractor
    {
        Task<List<string>> ExtractImageUrlList(CQ descCq);
    }
}
