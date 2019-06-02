﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.SqlServer;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using SampleShared;
using Serilog;

namespace SQLServerProducerLinq
{
    class Program
    {
        static void Main(string[] args)
        {
            //we are using serilog for sample purposes; any https://github.com/damianh/LibLog provider can be used
            var log = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Debug()
                .CreateLogger();
            Log.Logger = log;
            log.Information("Startup");
            log.Information(SharedConfiguration.AllSettings);

            var queueName = ConfigurationManager.AppSettings.ReadSetting("QueueName");
            var connectionString = ConfigurationManager.AppSettings.ReadSetting("Database");

            //create the container for creating a new queue
            using (var createQueueContainer = new QueueCreationContainer<SqlServerMessageQueueInit>(serviceRegister =>
                Injectors.AddInjectors(log, SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLiteProducer", serviceRegister)))
            {
                using (var createQueue =
                    createQueueContainer.GetQueueCreation<SqlServerMessageQueueCreation>(queueName, connectionString))
                {
                    //Create the queue if it doesn't exist
                    if (!createQueue.QueueExists)
                    {
                        //queue options
                        createQueue.Options.EnableDelayedProcessing = true;
                        createQueue.Options.EnableHeartBeat = true;
                        createQueue.Options.EnableMessageExpiration = true;
                        createQueue.Options.EnableStatus = true;
                        createQueue.Options.EnableStatusTable = true;
                        var result = createQueue.CreateQueue();
                        log.Information(result.Status.ToString());
                    }
                    else log.Information("Queue already exists; not creating");
                }
            }

            //create the producer
            using (var queueContainer = new QueueContainer<SqlServerMessageQueueInit>(serviceRegister =>
                Injectors.AddInjectors(log, SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLiteProducer", serviceRegister)))
            {
                using (var queue = queueContainer.CreateMethodProducer(queueName, connectionString))
                {
                    RunProducer.RunLoop(queue, ExpiredData, ExpiredDataFuture);
                }
            }

            //if jaeger is using udp, sometimes the messages get lost; there doesn't seem to be a flush() call ?
            if (SharedConfiguration.EnableTrace)
                System.Threading.Thread.Sleep(2000);
        }

        /// <summary>
        /// Creates an expired message by having it expire 1 second in the future and delaying processing for 5 seconds
        /// </summary>
        /// <returns></returns>
        private static IAdditionalMessageData ExpiredData()
        {
            var data = new AdditionalMessageData();
            data.SetExpiration(TimeSpan.FromSeconds(1));
            data.SetDelay(TimeSpan.FromSeconds(5));
            return data;
        }

        /// <summary>
        /// Creates an expired message 24 hours from now
        /// </summary>
        /// <returns></returns>
        private static IAdditionalMessageData ExpiredDataFuture()
        {
            var data = new AdditionalMessageData();
            data.SetExpiration(TimeSpan.FromDays(1));
            return data;
        }
    }
}