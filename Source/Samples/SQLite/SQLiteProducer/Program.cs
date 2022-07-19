using System;
using System.Configuration;
using System.IO;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic;
using SampleShared;
using Serilog;

namespace SQLiteProducer
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

            //determine our file path
            var fileLocation = Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), "Documents");
            var queueName = ConfigurationManager.AppSettings.ReadSetting("QueueName");
            var connectionString =
                $"Data Source={fileLocation}{ConfigurationManager.AppSettings.ReadSetting("Database")};Version=3;";
            var queueConnection = new QueueConnection(queueName, connectionString);
            //create the container for creating a new queue
            using (var createQueueContainer = new QueueCreationContainer<SqLiteMessageQueueInit>(serviceRegister =>
                Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLiteProducer", serviceRegister)
                , options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var createQueue =
                    createQueueContainer.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection))
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
            using (var queueContainer = new QueueContainer<SqLiteMessageQueueInit>(serviceRegister =>
                Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLiteProducer", serviceRegister),
                options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var queue = queueContainer.CreateProducer<SimpleMessage>(queueConnection))
                {
                    using (var admin = queueContainer.CreateAdminApi())
                    {
                        admin.AddQueueConnection(queueContainer, queueConnection);
                        RunProducer.RunLoop(queue, ExpiredData, ExpiredDataFuture, DelayedProcessing, admin);
                    }
                }
            }

            //if jaeger is using udp, sometimes the messages get lost; there doesn't seem to be a flush() call ?
            if (SharedConfiguration.EnableTrace)
                System.Threading.Thread.Sleep(2000);
        }

        /// <summary>
        /// Creates an expired Sqlite message by having it expire 1 second in the future and delaying processing for 5 seconds
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
