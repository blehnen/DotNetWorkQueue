using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Messages;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.UserDequeue
{
    public class UserDeQueueTestsShared
    {
        public void RunTest<TTransportInit, TMessage>(QueueConnection queueConnection,
           bool addInterceptors,
           int messageCount,
           ILogger logProvider,
           Func<QueueProducerConfiguration, int, AdditionalMessageData> generateData,
           Action<QueueConnection, QueueProducerConfiguration, long, int, ICreationScope> verify,
           bool sendViaBatch,
           List<int> userValues,
           int runTime,
           int timeOut,
           int readerCount,
           TimeSpan heartBeatTime,
           TimeSpan heartBeatMonitorTime,
           ICreationScope scope,
           string updateTime, bool enableChaos,
           Action<QueueConsumerConfiguration, int> setQueueOptions)
           where TTransportInit : ITransportInit, new()
           where TMessage : class
        {
            //add data with user column
            Parallel.ForEach(userValues, userColumn =>
            {
                RunTest<TTransportInit, TMessage>(queueConnection, addInterceptors,
                    messageCount, logProvider, generateData, verify, sendViaBatch, userColumn, scope, false);
            });

            //run a consumer for each data value
            using (var schedulerCreator = new SchedulerContainer())
            {
                var taskScheduler = schedulerCreator.CreateTaskScheduler();

                taskScheduler.Configuration.MaximumThreads = userValues.Count * 2;

                taskScheduler.Start();
                var taskFactory = schedulerCreator.CreateTaskFactory(taskScheduler);

                //spin up and process each value
                Parallel.ForEach(userValues, userColumn =>
                {
                    var consumer = new ConsumerAsyncShared<TMessage> { Factory = taskFactory };

                    consumer.RunConsumer<TTransportInit>(queueConnection, addInterceptors,
                        logProvider, runTime, messageCount, timeOut, readerCount, heartBeatTime, heartBeatMonitorTime, updateTime, enableChaos, scope, null, (g) => setQueueOptions(g, userColumn));
                });
            }
        }

        private void RunTest<TTransportInit, TMessage>(QueueConnection queueConnection,
            bool addInterceptors,
            long messageCount,
            ILogger logProvider,
            Func<QueueProducerConfiguration, int, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, int, ICreationScope> verify,
            bool sendViaBatch,
            int userValue,
            ICreationScope scope, bool enableChaos)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
        {
            //generate the test data
            var producer = new ProducerShared();
            producer.RunTest<TTransportInit, TMessage>(queueConnection,
                addInterceptors,
                messageCount,
                logProvider,
                g => generateData(g, userValue),
                (a, b, c, d) => verify(a, b, c, userValue, scope),
                sendViaBatch,
                false,
                scope, enableChaos);
        }
    }
}
