using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CsQuery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProxyProvider.HttpClientProvider;
using TaskQueue;
using TaskQueue.CommonTaskQueues.Handlers.CrawlerTaskHandler;
using TaskQueue.CommonTaskQueues.Handlers.DownloadTaskHandler;
using TaskQueue.Constants;

namespace WorldCollector.Yande.Handlers
{
    public class
        YandeListTaskHandler : CrawlerTaskHandler<YandeListTaskHandlerOptions,
            YandeListTaskData>
    {

        public YandeListTaskHandler(IOptions<YandeListTaskHandlerOptions> options,
            ITaskDistributor taskDistributor, IHttpClientProvider httpClientProvider, ILoggerFactory loggerFactory) :
            base(options, taskDistributor,
                httpClientProvider, loggerFactory)
        {
        }

        protected override async Task<TaskDataExecutionResult> HandleInternal(YandeListTaskData data,
            CancellationToken ct)
        {
            var client = await GetHttpClient();
            var listUrl = string.Format(Options.Value.ListUrlTpl, data.Page);
            var html = await client.GetStringAsync(listUrl);
            var cQuery = new CQ(html);
            var posts = cQuery["#post-list-posts li"];
            var imageUrls = new List<string>();
            var newTaskData = new List<TaskData>();
            var stop = false;
            foreach (var post in posts)
            {
                var id = int.Parse(post.Attributes["id"].Substring(1));
                if (data.LastImageIds?.Contains(id) == true)
                {
                    stop = true;
                    break;
                }

                var url = post.Cq().Children("a").Attr("href");
                if (url.StartsWith("//"))
                {
                    url = "https:" + url;
                }

                imageUrls.Add(url);
                data.CrawledImageIds.Add(id);
            }

            if (!stop)
            {
                newTaskData.Add(new YandeListTaskData
                    {Page = data.Page + 1, LastImageIds = data.LastImageIds});
            }

            if (imageUrls.Any())
            {
                newTaskData.AddRange(imageUrls.Select(t =>
                {
                    var newData = new DownloadTaskData {Url = t};
                    var filename = WebUtility.UrlDecode(t.Substring(t.LastIndexOf('/') + 1));
                    var regexSearch = new string(Path.GetInvalidFileNameChars()) +
                                      new string(Path.GetInvalidPathChars());
                    var r = new Regex($"[{Regex.Escape(regexSearch)}]");
                    newData.RelativeFilename = r.Replace(filename, "");
                    return newData;
                }));
            }

            await TaskDistributor.Distribute(newTaskData);
            return TaskDataExecutionResult.Complete;
        }
    }
}