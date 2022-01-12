using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.LiteDb;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using SampleShared;
using Serilog;
using Serilog.Core;

namespace LiteDbProducerConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            //we are using serilog for sample purposes
            var log = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .MinimumLevel.Debug()
                .CreateLogger();
            Log.Logger = log;
            log.Information("Startup");
            log.Information(SharedConfiguration.AllSettings);

            var userSelection = -1;
            var selection = @"
1) Direct Connection
2) Shared Connection
3) In-memory Connection
";
            while (userSelection < 0)
            {
                Console.WriteLine(selection);
                var key = Console.ReadKey(true);
                switch (key.KeyChar)
                {
                    case '1':
                        userSelection = 1;
                        break;
                    case '2':
                        userSelection = 2;
                        break;
                    case '3':
                        userSelection = 3;
                        break;
                }
            }


            //determine our file path
            var fileLocation = Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), "Documents");
            var queueName = ConfigurationManager.AppSettings.ReadSetting("QueueName");

            string connectionString = string.Empty;
            switch (userSelection)
            {
                case 1:
                    connectionString = $"Filename={fileLocation}{ConfigurationManager.AppSettings.ReadSetting("Database")};Connection=direct;";
                    break;
                case 2:
                    connectionString = $"Filename={fileLocation}{ConfigurationManager.AppSettings.ReadSetting("Database")};Connection=shared;";
                    break;
                case 3:
                    connectionString = ":memory:";
                    break;
            }

            ICreationScope scope = null; //contains a direct or memory connection that can be passed to other instances
            var queueConnection = new QueueConnection(queueName, connectionString);
            //create the container for creating a new queue
            using (var createQueueContainer = new QueueCreationContainer<LiteDbMessageQueueInit>(serviceRegister =>
                    Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace,
                        SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression,
                        SharedConfiguration.EnableEncryption, "LiteDbProducer", serviceRegister)
                , options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var createQueue =
                    createQueueContainer.GetQueueCreation<LiteDbMessageQueueCreation>(queueConnection))
                {
                    scope = createQueue.Scope;
                    //Create the queue if it doesn't exist
                    if (!createQueue.QueueExists)
                    {
                        //queue options
                        createQueue.Options.EnableDelayedProcessing = true;
                        createQueue.Options.EnableMessageExpiration = true;
                        createQueue.Options.EnableStatusTable = true;
                        var result = createQueue.CreateQueue();
                        log.Information(result.Status.ToString());
                    }
                    else log.Information("Queue already exists; not creating");
                }

                //create the consumer and the producer
                using (var queueContainer = new QueueContainer<LiteDbMessageQueueInit>(
                    x => RegisterService(x, log, scope),
                    options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
                {
                    using (var consumeQueue = queueContainer.CreateConsumer(queueConnection))
                    {
                        //set some processing options and start looking for work
                        consumeQueue.Configuration.Worker.WorkerCount = 4; //lets run 4 worker threads
                        consumeQueue.Configuration.HeartBeat.UpdateTime =
                            "sec(*%10)"; //set a heartbeat every 10 seconds
                        consumeQueue.Configuration.HeartBeat.MonitorTime =
                            TimeSpan.FromSeconds(15); //check for dead records every 15 seconds
                        consumeQueue.Configuration.HeartBeat.Time =
                            TimeSpan.FromSeconds(35); //records with no heartbeat after 35 seconds are considered dead

                        //an invalid data exception will be re-tried 3 times, with delays of 3, 6 and then finally 9 seconds
                        consumeQueue.Configuration.TransportConfiguration.RetryDelayBehavior.Add(
                            typeof(InvalidDataException),
                            new List<TimeSpan>
                                {TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(9)});

                        consumeQueue.Configuration.MessageExpiration.Enabled = true;
                        consumeQueue.Configuration.MessageExpiration.MonitorTime =
                            TimeSpan.FromSeconds(20); //check for expired messages every 20 seconds
                        consumeQueue.Start<SimpleMessage>(MessageProcessing.HandleMessages);

                        using (var queue = queueContainer.CreateProducer<SimpleMessage>(queueConnection))
                        {
                            RunProducer.RunLoop(queue, ExpiredData, ExpiredDataFuture, DelayedProcessing);
                        }
                    }
                }
            }

            //dispose of direct or memory connection (if present, noop otherwise)
            scope?.Dispose();

            //if jaeger is using udp, sometimes the messages get lost; there doesn't seem to be a flush() call ?
            if(SharedConfiguration.EnableTrace)
                System.Threading.Thread.Sleep(2000);
        }

        private static void RegisterService(IContainer container, Logger log, ICreationScope scope)
        {
            Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace,
                SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression,
                SharedConfiguration.EnableEncryption, "LiteDbProducerConsumer", container);

            //scope must be injected to share a direct or memory connection
            container.RegisterNonScopedSingleton(scope);
        }

        /// <summary>
        /// Creates an expired LiteDb message by having it expire 1 second in the future and delaying processing for 5 seconds
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

        private static IAdditionalMessageData DelayedProcessing(int seconds)
        {
            var data = new AdditionalMessageData();
            data.SetDelay(TimeSpan.FromSeconds(seconds));
            data.SetExpiration(TimeSpan.FromDays(1));
            return data;
        }
    }
}
