using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Interceptors;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    internal static class SharedSetup
    {
        public static QueueContainer<TTransportInit> CreateCreator<TTransportInit>(Action<IContainer> additionalRegs)
          where TTransportInit : ITransportInit, new()
        {
            return new QueueContainer<TTransportInit>(additionalRegs);
        }

        public static QueueContainer<TTransportInit> CreateCreator<TTransportInit>(InterceptorAdding addInterceptors, IMetrics metrics, bool enableChaos)
           where TTransportInit : ITransportInit, new()
        {
            switch (addInterceptors)
            {
                case InterceptorAdding.ConfigurationOnly:
                    return new QueueContainer<TTransportInit>(serviceRegister => serviceRegister.Register(() => metrics,
                       LifeStyles.Singleton).Register(() => new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                           Convert.FromBase64String("aaaaaaaaaaa=")), LifeStyles.Singleton), options => SetOptions(options, enableChaos));
                case InterceptorAdding.Yes:
                    return new QueueContainer<TTransportInit>(serviceRegister => serviceRegister.Register(() => metrics,
                        LifeStyles.Singleton).RegisterCollection<IMessageInterceptor>(new[]
                        {
                            typeof (GZipMessageInterceptor), //gzip compression
                            typeof (TripleDesMessageInterceptor) //encryption
                        }).Register(() => new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                            Convert.FromBase64String("aaaaaaaaaaa=")), LifeStyles.Singleton), options => SetOptions(options, enableChaos));
                default:
                    return new QueueContainer<TTransportInit>(serviceRegister => serviceRegister.Register(() => metrics,
                        LifeStyles.Singleton), options => SetOptions(options, enableChaos));
            }
        }

        public static void SetOptions(IContainer obj, bool enableChaos)
        {
            var policy = obj.GetInstance<IPolicies>();
            policy.EnableChaos = enableChaos;
        }

        public static QueueContainer<TTransportInit> CreateCreator<TTransportInit>(InterceptorAdding addInterceptors,
            ILogProvider logProvider, IMetrics metrics, bool createBadSerialization, bool enableChaos)
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
                                .Register(() => metrics,
                                    LifeStyles.Singleton).Register(() => new TripleDesMessageInterceptorConfiguration(
                                    Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                                    Convert.FromBase64String("aaaaaaaaaaa=")), LifeStyles.Singleton),
                            options => SetOptions(options, enableChaos));
                    case InterceptorAdding.Yes:
                        return new QueueContainer<TTransportInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)
                                .Register<ISerializer, SerializerThatWillCrashOnDeSerialization>(LifeStyles.Singleton)
                                .Register(() => metrics,
                                    LifeStyles.Singleton).RegisterCollection<IMessageInterceptor>(new[]
                                {
                                    typeof(GZipMessageInterceptor), //gzip compression
                                    typeof(TripleDesMessageInterceptor) //encryption
                                }).Register(() => new TripleDesMessageInterceptorConfiguration(
                                    Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                                    Convert.FromBase64String("aaaaaaaaaaa=")), LifeStyles.Singleton),
                            options => SetOptions(options, enableChaos));
                    default:
                        return new QueueContainer<TTransportInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)
                                .Register<ISerializer, SerializerThatWillCrashOnDeSerialization>(LifeStyles.Singleton)
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
                                LifeStyles.Singleton).Register(() => new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                                Convert.FromBase64String("aaaaaaaaaaa=")), LifeStyles.Singleton), options => SetOptions(options, enableChaos));
                    case InterceptorAdding.Yes:
                        return new QueueContainer<TTransportInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton).Register(() => metrics,
                                LifeStyles.Singleton).RegisterCollection<IMessageInterceptor>(new[]
                            {
                                typeof (GZipMessageInterceptor), //gzip compression
                                typeof (TripleDesMessageInterceptor) //encryption
                            }).Register(() => new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                                Convert.FromBase64String("aaaaaaaaaaa=")), LifeStyles.Singleton), options => SetOptions(options, enableChaos));
                    default:
                        return new QueueContainer<TTransportInit>(
                            serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton).Register(() => metrics,
                                LifeStyles.Singleton), options => SetOptions(options, enableChaos));
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
            if(!string.IsNullOrEmpty(route))
                configuration.Routes.Add(route);
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

    public enum LinqMethodTypes
    {
        Compiled,
#if NETFULL
        Dynamic
#endif
    }
}
