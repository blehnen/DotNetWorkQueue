// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.IntegrationTests.Shared.Route
{
    public class RouteTestsShared
    {
        public void RunTest<TTransportInit, TMessage>(string queueName,
           string connectionString,
           bool addInterceptors,
           int messageCount,
           ILogProvider logProvider,
           Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
           Action<string, string, QueueProducerConfiguration, long, string, ICreationScope> verify,
           bool sendViaBatch, 
           List<string> routes,
           int runTime, 
           int timeOut,
           int readerCount,
           TimeSpan heartBeatTime,
           TimeSpan heartBeatMonitorTime,
           ICreationScope scope,
           string updateTime)
           where TTransportInit : ITransportInit, new()
           where TMessage : class
        {
            //add data with routes - generate data per route passed in
            foreach(var route in routes)
            {
                RunTest<TTransportInit, TMessage>(queueName, connectionString, addInterceptors,
                    messageCount, logProvider, generateData, verify, sendViaBatch, route, scope);
            }

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

                    consumer.RunConsumer<TTransportInit>(queueName, connectionString, addInterceptors,
                        logProvider, runTime, messageCount, timeOut, readerCount, heartBeatTime, heartBeatMonitorTime, updateTime, new List<string> { route });
                });
            }
        }

        private void RunTest<TTransportInit, TMessage>(string queueName,
            string connectionString,
            bool addInterceptors,
            long messageCount,
            ILogProvider logProvider,
            Func<QueueProducerConfiguration, AdditionalMessageData> generateData,
            Action<string, string, QueueProducerConfiguration, long, string, ICreationScope> verify,
            bool sendViaBatch,
            string route,
            ICreationScope scope)
            where TTransportInit : ITransportInit, new()
            where TMessage : class
        {
            //generate the test data
            var producer = new ProducerShared();
            producer.RunTest<TTransportInit, TMessage>(queueName,
                connectionString,
                addInterceptors,
                messageCount,
                logProvider,
                g => GenerateDataWithRoute(generateData, g, route),
                (a, b, c, d, e) => VerifyRoutes(verify, a, b, c, d, route, scope),
                sendViaBatch,
                false,
                scope);
        }

        private AdditionalMessageData GenerateDataWithRoute(Func<QueueProducerConfiguration, AdditionalMessageData> generateData, QueueProducerConfiguration config, string route)
        {
            var data = generateData(config);
            data.Route = route;
            return data;
        }

        private void VerifyRoutes(Action<string, string, QueueProducerConfiguration, long, string, ICreationScope> verify, string arg1, string arg2, QueueProducerConfiguration arg3, long arg4, string route, ICreationScope scope)
        {
            verify(arg1, arg2, arg3, arg4, route, scope);
        }
    }
}
