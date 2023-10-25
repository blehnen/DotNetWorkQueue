using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Extensions.Configuration;
using App.Metrics.Formatters.InfluxDB;
using App.Metrics.Reporting.InfluxDB;
using App.Metrics.Scheduling;
using DotNetWorkQueue;
using DotNetWorkQueue.Interceptors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;
using IMetrics = DotNetWorkQueue.IMetrics;

namespace SampleShared
{
    public static class Injectors
    {
        private static DotNetWorkQueue.AppMetrics.Metrics _metrics;
        private static ActivitySource _tracer;
        private static AppMetricsTaskScheduler _metricScheduler;

        public static void AddInjectors(ILoggerFactory logFactory,
            bool addTrace,
            bool addMetrics,
            bool enableGzip,
            bool enableEncryption,
            string appName,
            IContainer container)
        {
            container.Register<ILoggerFactory>(() => logFactory, LifeStyles.Singleton);
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
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("tracesettings.json")
                .Build()
                .GetSection("Jaeger");

            var openTelemetry = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(configuration["JAEGER_SERVICE_NAME"]))
                .AddSource(configuration["JAEGER_SERVICE_NAME"], configuration["JAEGER_SERVICE_NAME"])
                .AddOtlpExporter(o =>
                {
                    var host = configuration["JAEGER_AGENT_HOST"];
                    var port = int.Parse(configuration["JAEGER_AGENT_PORT"]);
                    o.Endpoint = new Uri($"http://{host}:{port}");

                    // Using Batch Exporter (which is default)
                    // The other option is ExportProcessorType.Simple
                    o.ExportProcessorType = ExportProcessorType.Batch;
                    o.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>()
                    {
                        MaxQueueSize = 2048,
                        ScheduledDelayMilliseconds = 5000,
                        ExporterTimeoutMilliseconds = 30000,
                        MaxExportBatchSize = 512,
                    };
                })
                .Build();

            _tracer = new ActivitySource(configuration["JAEGER_SERVICE_NAME"]);
            container.RegisterNonScopedSingleton(_tracer);
        }
    }
}
