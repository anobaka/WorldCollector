using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bootstrap.Extensions;
using CsQuery.ExtensionMethods;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProxyProvider.HttpClientProvider;
using TaskQueue;
using TaskQueue.CommonTaskQueues.Handlers.DownloadTaskHandler;
using TaskQueue.Constants;
using WorldCollector.Yande.Handlers;
using WorldCollector.Yande.Models;

namespace WorldCollector.Yande
{
    public abstract class YandeCollector : TaskQueuePool<YandeCollectorOptions>
    {
        protected readonly IServiceProvider ServiceProvider;
        private List<int> _lastImageIds = new List<int>();
        private readonly List<int> _currentImageIds = new List<int>();
        public string Site => Options.Value.Site;
        public const string ListQueueId = "YandeList";
        public const string DownloadQueueId = "YandeDownload";

        protected YandeCollector(IOptions<YandeCollectorOptions> options, ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider) : base(options,
            loggerFactory)
        {
            ServiceProvider = serviceProvider;
        }

        public override async Task Stop()
        {
            await base.Stop();
            _lastImageIds.Clear();
            _currentImageIds.Clear();
        }

        public List<string> GetState()
        {
            var taskDataList = TaskDataVault;
            var listQueue = Queues.FirstOrDefault(t => t.Id == ListQueueId);
            var downloadQueue = Queues.FirstOrDefault(t => t.Id == DownloadQueueId);
            var listTaskDataList = taskDataList.OfType<YandeListTaskData>().ToList();
            var downloadTaskDataList = taskDataList.OfType<DownloadTaskData>().ToList();
            var listSuccess = listTaskDataList.Count(a => a.ExecutionResult == TaskDataExecutionResult.Complete);
            var listFailed = listTaskDataList.Sum(a => a.TryTimes - 1);
            var downloadSuccess =
                downloadTaskDataList.Count(a => a.ExecutionResult == TaskDataExecutionResult.Complete);
            var downloadFailed = downloadTaskDataList.Sum(a => Math.Max(a.TryTimes - 1, 0));
            var table = new List<List<string>>
            {
                new List<string>
                {
                    $"[{Status}]", 
                    $"Last Ids: {_lastImageIds.Count}"
                },
                new List<string>
                {
                    "[List]", 
                    $"Threads: {listQueue?.ActiveThreadCount ?? 0}/{listQueue?.MaxThreadCount ?? 0}",
                    $"Progress: {listSuccess}/{listTaskDataList.Count}", 
                    $"Failed: {listFailed}"
                },
                new List<string>
                {
                    "[Image]",
                    $"Threads: {downloadQueue?.ActiveThreadCount ?? 0}/{downloadQueue?.MaxThreadCount ?? 0}",
                    $"Progress: {downloadSuccess}/{downloadTaskDataList.Count}",
                    $"Skipped: {downloadTaskDataList.Count(a => a.DownloadResult == DownloadTaskDataResult.Skipped)}",
                    $"Failed: {downloadFailed}"
                }
            };
            return table.BeautifyTable();
        }

        public abstract Task<YandeCollectRecord> GetLastRecord();
        public abstract Task SaveCollectRecord(YandeCollectRecord record);

        public override async Task Start()
        {
            var lastRecord = await GetLastRecord();
            if (!string.IsNullOrEmpty(lastRecord?.Ids))
            {
                _lastImageIds = lastRecord.Ids.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse).ToList();
            }

            _currentImageIds.Clear();

            if (!Queues.Any())
            {
                var queues = ServiceProvider.GetRequiredService<IEnumerable<ITaskQueue>>()
                    .Where(t => t.Id.Contains("Yande"));
                queues.ForEach(AddQueue);
            }

            await base.Start();

            await Distribute(new YandeListTaskData {Page = 1, LastImageIds = _lastImageIds});

            while (Active)
            {
                await Task.Delay(1000);
            }

            if (Status == TaskQueueStatus.Running)
            {
                await base.Stop();

                if (_currentImageIds.Any())
                {
                    var record = new YandeCollectRecord
                    {
                        CollectDt = DateTime.Now,
                        Ids = string.Join(",", _currentImageIds),
                        Site = Options.Value.Site
                    };
                    await SaveCollectRecord(record);
                }
            }
        }
    }
}