using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CsQuery;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ProxyProvider.HttpClientProvider;
using TaskQueue;
using TaskQueue.CommonTaskQueues.Handlers.CrawlerTaskHandler;
using TaskQueue.CommonTaskQueues.Handlers.DownloadTaskHandler;

namespace WorldCollector.Weibo.Collectors.AlbumImageCollector.TaskHandlers
{
    public class
        WeiboPhotoListTaskHandler : CrawlerTaskHandler<WeiboPhotoListTaskHandlerOptions, WeiboPhotoListTaskData>
    {
        public WeiboPhotoListTaskHandler(IOptions<WeiboPhotoListTaskHandlerOptions> options,
            ITaskDistributor taskDistributor, IHttpClientProvider httpClientProvider) : base(options, taskDistributor,
            httpClientProvider)
        {
        }

        protected override async Task HandleInternalUnstatable(WeiboPhotoListTaskData taskData,
            CancellationToken cancellationChangeToken)
        {
            var url = string.Format(Options.Value.UrlTemplate, taskData.SinceId, taskData.Page,
                (DateTime.Now.ToUniversalTime() - new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc)).Milliseconds);
            var client = await GetHttpClient();
            var rsp = await client.GetAsync(url, cancellationChangeToken);
            var responseString = await rsp.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            var data = json["data"];
            if (!string.IsNullOrEmpty(data))
            {
                var html = new CQ(data);
                var imageData = html[".photo_cont>a.ph_ar_box>img.photo_pict"].Select(t => t.GetAttribute("src"))
                    .Where(t => !string.IsNullOrEmpty(t)).Select(t =>
                    {
                        var r = Regex.Replace(t, "/thumb\\d+?/", "/large/");
                        if (r.StartsWith("//"))
                        {
                            r = $"https:{r}";
                        }

                        var index = r.IndexOf('?');
                        if (index > -1)
                        {
                            r = r.Substring(0, index);
                        }

                        return r;
                    });
                var newTaskData = imageData.Select(a => (TaskData) new DownloadTaskData
                {
                    Url = a,
                    RelativeFilename = Path.GetFileName(a)
                }).ToList();
                var nextPageData = html["div[node-type='loading']"].Attr("action-data");
                if (!string.IsNullOrEmpty(nextPageData))
                {
                    var sinceIdMatch = Regex.Match(nextPageData, "since_id=(?<sinceId>[^&]+)");
                    if (sinceIdMatch.Success)
                    {
                        var sinceId = sinceIdMatch.Groups["sinceId"].Value;
                        newTaskData.Insert(0,
                            new WeiboPhotoListTaskData {Page = taskData.Page + 1, SinceId = sinceId});
                    }
                }

                await TaskDistributor.Distribute(newTaskData);
            }
        }
    }
}