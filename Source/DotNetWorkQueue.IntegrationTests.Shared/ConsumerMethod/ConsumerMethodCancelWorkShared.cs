using DotNetWorkQueue.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod
{
    public class ConsumerMethodCancelWorkShared<TTransportInit>
        where TTransportInit : ITransportInit, new()
    {
        private QueueConnection _queueConnection;
        private int _workerCount;
        private TimeSpan _heartBeatTime;
        private TimeSpan _heartBeatMonitorTime;
        private string _updatetime;
        private IConsumerMethodQueue _queue;
        private QueueContainer<TTransportInit> _badQueueContainer;
        private Action<IContainer> _badQueueAdditions;
        private ILogger _logger;

        public void RunConsumer(QueueConnection queueConnection, bool addInterceptors,
            ILogger logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut, Action<IContainer> badQueueAdditions,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, string updateTime, Guid id, bool enableChaos, ICreationScope scope)
        {
            _queueConnection = queueConnection;
            _workerCount = workerCount;
            _badQueueAdditions = badQueueAdditions;
            _updatetime = updateTime;
            _logger = logProvider;

            _heartBeatTime = heartBeatTime;
            _heartBeatMonitorTime = heartBeatMonitorTime;

            _queue = CreateConsumerInternalThread(scope);
            var t = new Thread(RunBadQueue);
            t.Start();

            if (enableChaos)
                timeOut *= 2;

            //run consumer
            RunConsumerInternal(queueConnection, addInterceptors, logProvider, runTime,
                messageCount, workerCount, timeOut, _queue, heartBeatTime, heartBeatMonitorTime, id, updateTime, enableChaos, scope);
        }


        private void RunConsumerInternal(QueueConnection queueConnection, bool addInterceptors,
            ILogger logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut, IDisposable queueBad,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, Guid id, string updateTime, bool enableChaos, ICreationScope scope)
        {

            using (var trace = SharedSetup.CreateTrace("consumer-cancel"))
            {
                using (var metrics = new Metrics.Metrics(queueConnection.Queue))
                {
                    var addInterceptorConsumer = InterceptorAdding.No;
                    if (addInterceptors)
                    {
                        addInterceptorConsumer = InterceptorAdding.ConfigurationOnly;
                    }

                    using (
                        var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider,
                            metrics, false, enableChaos, scope, trace.Source)
                    )
                    {

                        using (
                            var queue =
                            creator.CreateMethodConsumer(queueConnection, x => x.RegisterNonScopedSingleton(scope).RegisterNonScopedSingleton(trace.Source)))
                        {
                            SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                                heartBeatMonitorTime, updateTime, null);
                            queue.Start(CreateNotifications.Create(logProvider));

                            var time = runTime * 1000 / 2;
                            Thread.Sleep(time);
                            queueBad.Dispose();
                            _badQueueContainer.Dispose();

                            var counter = 0;
                            var counterLess = timeOut / 2;
                            while (counter < counterLess)
                            {
                                if (MethodIncrementWrapper.Count(id) >= messageCount)
                                {
                                    break;
                                }

                                Thread.Sleep(1000);
                                counter++;
                            }

                            //wait for commits in transport...
                            Thread.Sleep(3000);
                        }

                        var count = MethodIncrementWrapper.Count(id);
                        Assert.Equal(messageCount, count);
                        VerifyMetrics.VerifyProcessedCount(queueConnection.Queue, metrics.GetCurrentMetrics(),
                            messageCount);
                        LoggerShared.CheckForErrors(queueConnection.Queue);
                    }
                }
            }
        }

        private IConsumerMethodQueue CreateConsumerInternalThread(ICreationScope scope)
        {
            _badQueueContainer = SharedSetup.CreateCreator<TTransportInit>(_badQueueAdditions);

            var queue =
                _badQueueContainer.CreateMethodConsumer(_queueConnection, x => x.RegisterNonScopedSingleton(scope));

            SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, _workerCount, _heartBeatTime, _heartBeatMonitorTime, _updatetime, null);
            return queue;
        }

        private void RunBadQueue()
        {
            //start looking for work
            _queue.Start(CreateNotifications.Create(_logger));
        }
    }
}
