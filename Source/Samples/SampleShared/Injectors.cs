using System;
using System.IO;
using System.Net.Configuration;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Extensions.Configuration;
using App.Metrics.Formatters.InfluxDB;
using App.Metrics.Reporting.InfluxDB;
using App.Metrics.Scheduling;
using DotNetWorkQueue;
using DotNetWorkQueue.Interceptors;
using Jaeger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTracing;
using IMetrics = DotNetWorkQueue.IMetrics;
using Logger = Serilog.Core.Logger;

namespace SampleShared
{
    public static class Injectors
    {
        private static DotNetWorkQueue.AppMetrics.Metrics _metrics;
        private static ITracer _tracer;
        private static AppMetricsTaskScheduler _metricScheduler;

        public static void AddInjectors(Logger log,
            bool addTrace,
            bool addMetrics,
            bool enableGzip,
            bool enableEncryption,
            string appName,
            IContainer container)
        {
            container.Register(() => log, LifeStyles.Singleton);
            if (addMetrics)
            {
                AddMetrics(container, appName);
            }

            if (addTrace)
            {
                AddTrace(container);
            }

            if (enableGzip || enableEncryption)
            {
                AddMessageInterceptors(container, enableEncryption, enableGzip);
            }

        }

        public static void SetOptions(IContainer container, bool enableChaos)
        {
            var pol = container.GetInstance<IPolicies>();
            pol.EnableChaos = enableChaos;
        }

        private static void AddMessageInterceptors(IContainer container,
            bool des, bool gzip)
        {
            //encryption keys for sample only
            string key = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            string iv = "aaaaaaaaaaa=";

            if (des && gzip)
            {
                var desConfiguration = new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String(key), Convert.FromBase64String(iv));
                container.RegisterCollection<IMessageInterceptor>(new[]
                {
                    typeof (GZipMessageInterceptor), //gzip compression
                    typeof (TripleDesMessageInterceptor) //encryption
                });
                container.Register(() => desConfiguration, LifeStyles.Singleton);
            }
            else if (gzip)
            {
                container.RegisterCollection<IMessageInterceptor>(new[]
                {
                    typeof (GZipMessageInterceptor) //gzip compression
                });
            }
            else if (des)
            {
                var desConfiguration = new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String(key), Convert.FromBase64String(iv));
                container.RegisterCollection<IMessageInterceptor>(new[]
                {
                    typeof (TripleDesMessageInterceptor) //encryption
                });
                container.Register(() => desConfiguration,
                    LifeStyles.Singleton);
            }
        }
        private static void AddMetrics(IContainer container, string appName)
        {
            if (_metrics != null)
            {
                container.RegisterNonScopedSingleton<IMetrics>(_metrics);
                return;
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("metricsettings.json")
                .Build();

            var influxOptions = new MetricsReportingInfluxDbOptions();
            configuration.GetSection(nameof(MetricsReportingInfluxDbOptions)).Bind(influxOptions);

            var metricsRoot = new MetricsBuilder()
                .Configuration.ReadFrom(configuration)
                        .Configuration.Configure(
                            options =>
                            {
                                options.AddServerTag();
                                options.AddAppTag(appName);
                            })
                .Report.ToInfluxDb(influxOptions)
                .Build();


            var metrics = new DotNetWorkQueue.AppMetrics.Metrics(metricsRoot);
            container.RegisterNonScopedSingleton<IMetrics>(metrics);

            var scheduler = new AppMetricsTaskScheduler(
                TimeSpan.FromSeconds(3),
                async () =>
                {
                    await Task.WhenAll(metricsRoot.ReportRunner.RunAllAsync());
                });
            scheduler.Start();
            _metricScheduler = scheduler;
            _metrics = metrics;
        }

        private static void AddTrace(IContainer container)
        {
            if (_tracer != null)
            {
                container.RegisterNonScopedSingleton(_tracer);
                return;
            }
            var loggerFactory = new LoggerFactory();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("tracesettings.json")
                .Build()
                .GetSection("Jaeger");
            var tracer = Configuration.FromIConfiguration(loggerFactory, configuration).GetTracer();
            container.RegisterNonScopedSingleton(tracer);
            _tracer = tracer;
        }
    }
}
