using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Bootstrap.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProxyProvider.HttpClientProvider;
using TaskQueue;
using TaskQueue.CommonTaskQueues.Handlers.DownloadTaskHandler;
using WorldCollector.Yande.Handlers;
using BindingFlags = System.Reflection.BindingFlags;

namespace WorldCollector.Yande
{
    public static class YandeServiceCollectionExtensions
    {
        private static IOptions<YandeCollectorOptions> _getOptions(this IServiceProvider s, string site) =>
            s.GetFirstRequiredService<IOptions<YandeCollectorOptions>>(t => t.Value.Site == site);

        public static IServiceCollection AddYandeCollector<TImplementation>(this IServiceCollection services,
            IConfigurationSection yandeCollectorConfigurationSection) where TImplementation : YandeCollector =>
            services.AddYandeCollector<TImplementation, TImplementation>(yandeCollectorConfigurationSection);

        public static IServiceCollection AddYandeCollector<TService, TImplementation>(this IServiceCollection services,
            IConfigurationSection yandeCollectorConfigurationSection) where TImplementation : YandeCollector, TService
            where TService : class
        {
            var options = yandeCollectorConfigurationSection.Get<YandeCollectorOptions>();
            var httpClient = new HttpClient();
            const string ua =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.163 Safari/537.36";
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
            var httpClientProvider = new StaticHttpClientProvider(httpClient);
            return services
                .AddSingleton(Options.Create(options))
                .AddSingleton<TService, TImplementation>(s =>
                {
                    var type = typeof(TImplementation);
                    var paramInfos = type.GetConstructors().FirstOrDefault(t => t.IsPublic)?.GetParameters();
                    var @params = paramInfos.Select(p =>
                        p.ParameterType == typeof(IOptions<YandeCollectorOptions>)
                            ? s._getOptions(options.Site)
                            : s.GetRequiredService(p.ParameterType)).ToArray();
                    return Activator.CreateInstance(type, @params, null) as TImplementation;
                })
                // .AddSingleton<YandeListTaskHandler>()
                .AddSingleton<ITaskQueue>(s =>
                {
                    var yandeCollectorOptions = s._getOptions(options.Site);
                    var listQueueOptions = Options.Create(new TaskQueueOptions
                        {Id = YandeCollector.ListQueueId, MaxThreads = yandeCollectorOptions.Value.ListThreads});
                    var listHandlerOptions = Options.Create(new YandeListTaskHandlerOptions
                    {
                        ListUrlTpl = yandeCollectorOptions.Value.ListUrlTemplate
                    });
                    var taskDistributor =
                        s.GetFirstRequiredService<TService>(t => t is TImplementation a && a.Site == options.Site) as
                            TImplementation;
                    var listHandler = new YandeListTaskHandler(listHandlerOptions,
                        taskDistributor, httpClientProvider, s.GetRequiredService<ILoggerFactory>());
                    return new TaskQueue<TaskQueueOptions, YandeListTaskHandler>(listQueueOptions, listHandler);
                })
                .AddSingleton<ITaskQueue>(s =>
                {
                    var yandeCollectorOptions = s._getOptions(options.Site);
                    var downloadQueueOptions = Options.Create(new TaskQueueOptions
                    {
                        MaxThreads = yandeCollectorOptions.Value.DownloadThreads, Id = YandeCollector.DownloadQueueId
                    });
                    var downloadHandlerOptions = Options.Create(new DownloadTaskHandlerOptions
                        {DownloadPath = yandeCollectorOptions.Value.DownloadPath});
                    var taskDistributor =
                        s.GetFirstRequiredService<TService>(t => t is TImplementation a && a.Site == options.Site) as
                            TImplementation;
                    return new TaskQueue<TaskQueueOptions, DownloadTaskHandler>(downloadQueueOptions,
                        new DownloadTaskHandler(downloadHandlerOptions, taskDistributor, httpClientProvider,
                            null, s.GetRequiredService<ILoggerFactory>()));
                });
        }
    }
}