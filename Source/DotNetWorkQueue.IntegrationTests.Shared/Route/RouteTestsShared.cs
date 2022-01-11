using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared.Route
{
    public class RouteTestsShared
    {
        public void RunTest<TTransportInit, TMessage>(QueueConnection queueConnection,
           bool addInterceptors,
           int messageCount,
           ILogger logProvider,
           Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
           Action<QueueConnection, QueueProducerConfiguration, long, string, ICreationScope> verify,
           bool sendViaBatch, 
           List<string> routes,
           int runTime, 
           int timeOut,
           int readerCount,
           TimeSpan heartBeatTime,
           TimeSpan heartBeatMonitorTime,
           ICreationScope scope,
           string updateTime, bool enableChaos)
           where TTransportInit : ITransportInit, new()
           where TMessage : class
        {
            //add data with routes - generate data per route passed in
            Parallel.ForEach(routes, route =>
            {
                RunTest<TTransportInit, TMessage>(queueConnection, addInterceptors,
                    messageCount, logProvider, generateData, verify, sendViaBatch, route, scope, false);
            });

            //run a consumer for each route
            using (var schedulerCreator = new SchedulerContainer())
            {
                var taskScheduler = schedulerCreator.CreateTaskScheduler();

                taskScheduler.Configuration.MaximumThreads = routes.Count * 2;
                taskScheduler.Configuration.MaxQueueSize = routes.Count;

                taskScheduler.Start();
                var taskFactory = schedulerCreator.CreateTaskFactory(taskScheduler);

                //spin up and process each route
                Parallel.ForEach(routes, route =>
                {
                    var consumer = new ConsumerAsyncShared<TMessage> {Factory = taskFactory};

                    consumer.RunConsumer<TTransportInit>(queueConnection, addInterceptors,
                        logProvider, runTime, messageCount, timeOut, readerCount, heartBeatTime, heartBeatMonitorTime, updateTime, enableChaos, scope, new List<string> { route });
                });
            }
        }

        private void RunTest<TTransportInit, TMessage>(QueueConnection queueConnection,
            bool addInterceptors,
            long messageCount,
            ILogger logProvider,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<QueueConnection, QueueProducerConfiguration, long, string, ICreationScope> verify,
            bool sendViaBatch,
            string route,
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
                g => GenerateDataWithRoute(generateData, g, route),
                (a, b, c, d) => VerifyRoutes(verify, a, b, c, route, scope),
                sendViaBatch,
                false,
                scope, enableChaos);
        }

        private AdditionalMessageData GenerateDataWithRoute(Func<QueueProducerConfiguration, AdditionalMessageData> generateData, QueueProducerConfiguration config, string route)
        {
            var data = generateData(config);
            data.Route = route;
            return data;
        }

        private void VerifyRoutes(Action<QueueConnection, QueueProducerConfiguration, long, string, ICreationScope> verify, QueueConnection arg1, QueueProducerConfiguration arg3, long arg4, string route, ICreationScope scope)
        {
            verify(arg1, arg3, arg4, route, scope);
        }
    }
}
