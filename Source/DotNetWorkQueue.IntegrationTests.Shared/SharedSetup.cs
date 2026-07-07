using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Interceptors;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    internal static class SharedSetup
    {
        public static QueueContainer<TTransportInit> CreateCreator<TTransportInit>(Action<IContainer> additionalRegs)
          where TTransportInit : ITransportInit, new()
        {
            return new QueueContainer<TTransportInit>(additionalRegs);
        }

        public static QueueContainer<TTransportInit> CreateCreator<TTransportInit>(InterceptorAdding addInterceptors, IMetrics metrics, bool enableChaos, ActivitySource trace)
           where TTransportInit : ITransportInit, new()
        {
            switch (addInterceptors)
            {
                case InterceptorAdding.ConfigurationOnly:
                    return new QueueContainer<TTransportInit>(serviceRegister => serviceRegister.Register(() => metrics,
                       LifeStyles.Singleton).Register(() => new AesMessageInterceptorConfiguration(System.Text.Encoding.ASCII.GetBytes("0123456789abcdef0123456789abcdef")                           ), LifeStyles.Singleton).RegisterNonScopedSingleton(trace), options => SetOptions(options, enableChaos));
                case InterceptorAdding.Yes:
                    return new QueueContainer<TTransportInit>(serviceRegister => serviceRegister.Register(() => metrics,
                        LifeStyles.Singleton).RegisterCollection<IMessageInterceptor>(new[]
                        {
                            typeof (GZipMessageInterceptor), //gzip compression
                            typeof (AesMessageInterceptor) //encryption
                        }).Register(() => new AesMessageInterceptorConfiguration(System.Text.Encoding.ASCII.GetBytes("0123456789abcdef0123456789abcdef")                            ), LifeStyles.Singleton).RegisterNonScopedSingleton(trace), options => SetOptions(options, enableChaos));
                default:
                    return new QueueContainer<TTransportInit>(serviceRegister => serviceRegister.Register(() => metrics,
                        LifeStyles.Singleton).RegisterNonScopedSingleton(trace), options => SetOptions(options, enableChaos));
            }
        }

        public static void SetOptions(IContainer obj, bool enableChaos)
        {
            var policy = obj.GetInstance<IPolicies>();
            policy.EnableChaos = enableChaos;
        }

        public static QueueContainer<TTransportInit> CreateCreator<TTransportInit>(InterceptorAdding addInterceptors,
            ILogger logProvider, IMetrics metrics, bool createBadSerialization, bool enableChaos, ICreationScope scope, ActivitySource trace)
            where TTransportInit : ITransportInit, new()
        {
            if (createBadSerialization)
            {
                switch (addInterceptors)
                {
                    case InterceptorAdding.ConfigurationOnly:
                        return new QueueContainer<TTransportInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)
                                .Register<ISerializer, SerializerThatWillCrashOnDeSerialization>(LifeStyles.Singleton)
                                .RegisterNonScopedSingleton(scope)
                                .RegisterNonScopedSingleton(trace)
                                .Register(() => metrics,
                                    LifeStyles.Singleton).Register(() => new AesMessageInterceptorConfiguration(System.Text.Encoding.ASCII.GetBytes("0123456789abcdef0123456789abcdef")), LifeStyles.Singleton),
                            options => SetOptions(options, enableChaos));
                    case InterceptorAdding.Yes:
                        return new QueueContainer<TTransportInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)
                                .Register<ISerializer, SerializerThatWillCrashOnDeSerialization>(LifeStyles.Singleton)
                                .RegisterNonScopedSingleton(scope)
                                .RegisterNonScopedSingleton(trace)
                                .Register(() => metrics,
                                    LifeStyles.Singleton).RegisterCollection<IMessageInterceptor>(new[]
                                {
                                    typeof(GZipMessageInterceptor), //gzip compression
                                    typeof(AesMessageInterceptor) //encryption
                                }).Register(() => new AesMessageInterceptorConfiguration(System.Text.Encoding.ASCII.GetBytes("0123456789abcdef0123456789abcdef")), LifeStyles.Singleton),
                            options => SetOptions(options, enableChaos));
                    default:
                        return new QueueContainer<TTransportInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)
                                .Register<ISerializer, SerializerThatWillCrashOnDeSerialization>(LifeStyles.Singleton)
                                .RegisterNonScopedSingleton(scope)
                                .RegisterNonScopedSingleton(trace)
                                .Register(() => metrics,
                                    LifeStyles.Singleton), options => SetOptions(options, enableChaos));
                }
            }
            else
            {
                switch (addInterceptors)
                {
                    case InterceptorAdding.ConfigurationOnly:
                        return new QueueContainer<TTransportInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton).Register(() => metrics,
                                LifeStyles.Singleton).Register(() => new AesMessageInterceptorConfiguration(System.Text.Encoding.ASCII.GetBytes("0123456789abcdef0123456789abcdef")                                ), LifeStyles.Singleton).RegisterNonScopedSingleton(scope).RegisterNonScopedSingleton(trace), options => SetOptions(options, enableChaos));
                    case InterceptorAdding.Yes:
                        return new QueueContainer<TTransportInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton).Register(() => metrics,
                                LifeStyles.Singleton).RegisterCollection<IMessageInterceptor>(new[]
                            {
                                typeof (GZipMessageInterceptor), //gzip compression
                                typeof (AesMessageInterceptor) //encryption
                            }).Register(() => new AesMessageInterceptorConfiguration(System.Text.Encoding.ASCII.GetBytes("0123456789abcdef0123456789abcdef")                                ), LifeStyles.Singleton).RegisterNonScopedSingleton(scope).RegisterNonScopedSingleton(trace), options => SetOptions(options, enableChaos));
                    default:
                        return new QueueContainer<TTransportInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton).Register(() => metrics,
                                LifeStyles.Singleton).RegisterNonScopedSingleton(scope).RegisterNonScopedSingleton(trace), options => SetOptions(options, enableChaos));
                }
            }
        }

        public static void SetupDefaultErrorRetry(QueueConsumerConfiguration configuration)
        {
            var times = new List<TimeSpan> { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) };
            configuration.TransportConfiguration.RetryDelayBehavior.Add(typeof(IndexOutOfRangeException),
                times);
        }

        public static void SetupDefaultConsumerQueue(QueueConsumerConfiguration configuration, int workerCount,
            TimeSpan heartbeatTime, TimeSpan heartbeatMonitorTime, string updateTime, string route)
        {
            configuration.HeartBeat.Time = heartbeatTime;
            configuration.HeartBeat.MonitorTime = heartbeatMonitorTime;
            configuration.HeartBeat.UpdateTime = updateTime;
            configuration.HeartBeat.ThreadPoolConfiguration.WaitForThreadPoolToFinish = TimeSpan.FromSeconds(5);
            configuration.HeartBeat.ThreadPoolConfiguration.ThreadsMax = 2;
            configuration.Worker.WorkerCount = workerCount;
            configuration.Worker.TimeToWaitForWorkersToStop = TimeSpan.FromSeconds(5);
            configuration.Worker.TimeToWaitForWorkersToCancel = TimeSpan.FromSeconds(10);
            configuration.Worker.SingleWorkerWhenNoWorkFound = true;
            if (!string.IsNullOrEmpty(route))
                configuration.Routes.Add(route);
        }

        public static void SetupDefaultConsumerQueueErrorPurge(QueueConsumerConfiguration configuration, bool actuallyPurge)
        {
            configuration.Worker.TimeToWaitForWorkersToStop = TimeSpan.FromSeconds(5);
            configuration.Worker.TimeToWaitForWorkersToCancel = TimeSpan.FromSeconds(10);
            configuration.Worker.SingleWorkerWhenNoWorkFound = true;
            configuration.MessageError.MessageAge = actuallyPurge ? TimeSpan.FromSeconds(0) : TimeSpan.FromDays(1);
            configuration.MessageError.Enabled = true;
            configuration.MessageError.MonitorTime = TimeSpan.FromSeconds(5);
        }

        public static ActivitySourceWrapper CreateTrace(string name, bool collectActivities = false)
        {
            var traceName = TraceSettings.TraceName(name);
            TracerProvider openTelemetry = null;
            if (TraceSettings.Enabled)
            {
                openTelemetry = Sdk.CreateTracerProviderBuilder()
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(traceName))
                    .AddSource(traceName, traceName)
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri($"http://{TraceSettings.Host}:{TraceSettings.Port}");

                        // Using Batch Exporter (which is default)
                        // The other option is ExportProcessorType.Simple
                        o.ExportProcessorType = ExportProcessorType.Batch;
                        o.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>()
                        {
                            MaxQueueSize = 16384,
                            ScheduledDelayMilliseconds = 5000,
                            ExporterTimeoutMilliseconds = 30000,
                            MaxExportBatchSize = 2048,
                        };
                    })
                    .Build();
            }
            return new ActivitySourceWrapper(new ActivitySource(traceName), collectActivities, openTelemetry);
        }
    }

    public class ActivitySourceWrapper : IDisposable
    {
        private readonly ActivityListener _listener;
        private readonly TracerProvider _provider;

        public ActivitySourceWrapper(ActivitySource source, bool collectActivities = false, TracerProvider provider = null)
        {
            Source = source;
            _provider = provider;

            // Listener is ALWAYS registered so trace decorator code paths execute
            // during integration tests (preserves the TraceExtensions coverage cascade).
            // Only the ActivityStarted callback (which populates CollectedActivities) is
            // gated by collectActivities to avoid ConcurrentBag overhead when tests
            // don't actually inspect the collected spans.
            _listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == source.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            if (collectActivities)
            {
                _listener.ActivityStarted = activity => CollectedActivities.Add(activity);
            }
            ActivitySource.AddActivityListener(_listener);
        }

        public ActivitySource Source
        {
            get;
        }

        public ConcurrentBag<Activity> CollectedActivities { get; } = new();

        public void Dispose()
        {
            _listener?.Dispose();
            Source?.Dispose();
            if (_provider != null)
            {
                _provider.ForceFlush(2000);
                _provider.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }

    public class MethodMessageProcessingCancel : IMessageMethodHandling
    {
        private readonly Guid _queueId;
        public MethodMessageProcessingCancel(Guid queueId)
        {
            _queueId = queueId;
        }
        public void Dispose()
        {

            GC.SuppressFinalize(this);
        }

        public void HandleExecution(IReceivedMessage<MessageExpression> receivedMessage,
            IWorkerNotification notification)
        {
            MethodIncrementWrapper.SetRollback(_queueId, (Guid)receivedMessage.CorrelationId.Id.Value);
            throw new OperationCanceledException("I don't feel like processing this message");
        }

        public bool IsDisposed => false;
    }
    public enum InterceptorAdding
    {
        Yes,
        ConfigurationOnly,
        No
    }

}
