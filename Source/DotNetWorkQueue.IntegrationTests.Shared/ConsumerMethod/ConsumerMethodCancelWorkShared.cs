using System;
using System.Threading;
using DotNetWorkQueue.Logging;
using Xunit;

namespace DotNetWorkQueue.IntegrationTests.Shared.ConsumerMethod
{
    public class ConsumerMethodCancelWorkShared<TTransportInit>
        where TTransportInit : ITransportInit, new()
    {
        private string _queueName;
        private string _connectionString;
        private int _workerCount;
        private TimeSpan _heartBeatTime;
        private TimeSpan _heartBeatMonitorTime;
        private string _updatetime;
        private IConsumerMethodQueue _queue;
        private QueueContainer<TTransportInit> _badQueueContainer;
        private Action<IContainer> _badQueueAdditions;

        public void RunConsumer(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut, Action<IContainer> badQueueAdditions,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, string updateTime, Guid id, bool enableChaos)
        {
            _queueName = queueName;
            _connectionString = connectionString;
            _workerCount = workerCount;
            _badQueueAdditions = badQueueAdditions;
            _updatetime = updateTime;

            _heartBeatTime = heartBeatTime;
            _heartBeatMonitorTime = heartBeatMonitorTime;

            _queue = CreateConsumerInternalThread();
            var t = new Thread(RunBadQueue);
            t.Start();

            if (enableChaos)
                timeOut *= 2;

            //run consumer
            RunConsumerInternal(queueName, connectionString, addInterceptors, logProvider, runTime,
                messageCount, workerCount, timeOut, _queue, heartBeatTime, heartBeatMonitorTime, id, updateTime, enableChaos);
        }


        private void RunConsumerInternal(string queueName, string connectionString, bool addInterceptors,
            ILogProvider logProvider,
            int runTime, int messageCount,
            int workerCount, int timeOut, IDisposable queueBad,
            TimeSpan heartBeatTime, TimeSpan heartBeatMonitorTime, Guid id, string updateTime, bool enableChaos)
        {

            using (var metrics = new Metrics.Metrics(queueName))
            {
                var addInterceptorConsumer = InterceptorAdding.No;
                if (addInterceptors)
                {
                    addInterceptorConsumer = InterceptorAdding.ConfigurationOnly; 
                }
                using (
                    var creator = SharedSetup.CreateCreator<TTransportInit>(addInterceptorConsumer, logProvider, metrics, false, enableChaos)
                    )
                {

                    using (
                        var queue =
                            creator.CreateMethodConsumer(queueName,
                                connectionString))
                    {
                        SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, workerCount, heartBeatTime,
                            heartBeatMonitorTime, updateTime, null);
                        queue.Start();

                        var time = runTime*1000/2;
                        Thread.Sleep(time);
                        queueBad.Dispose();
                        _badQueueContainer.Dispose();

                        var counter = 0;
                        var counterLess = timeOut/2;
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
                    VerifyMetrics.VerifyProcessedCount(queueName, metrics.GetCurrentMetrics(), messageCount);
                    LoggerShared.CheckForErrors(queueName);
                }
            }
        }

        private IConsumerMethodQueue CreateConsumerInternalThread()
        {
            _badQueueContainer = SharedSetup.CreateCreator<TTransportInit>(_badQueueAdditions);

            var queue =
                _badQueueContainer.CreateMethodConsumer(_queueName,
                    _connectionString);
 
                SharedSetup.SetupDefaultConsumerQueue(queue.Configuration, _workerCount, _heartBeatTime, _heartBeatMonitorTime, _updatetime, null);
                return queue;        
        }

        private void RunBadQueue()
        {
            //start looking for work
            _queue.Start();
        }
    }
}
