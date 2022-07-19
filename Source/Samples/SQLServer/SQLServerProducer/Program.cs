using System;
using System.Configuration;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.SqlServer;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using SampleShared;
using Serilog;

namespace SQLServerProducer
{
    class Program
    {
        private static bool _userData;

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

            var queueName = ConfigurationManager.AppSettings.ReadSetting("QueueName");
            var connectionString = ConfigurationManager.AppSettings.ReadSetting("Database");
            var queueConnection = new QueueConnection(queueName, connectionString);
            //create the container for creating a new queue
            using (var createQueueContainer = new QueueCreationContainer<SqlServerMessageQueueInit>(serviceRegister =>
                Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLServerProducer", serviceRegister),
                options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var createQueue =
                    createQueueContainer.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection))
                {
                    var enabledUserColumns = ConfigurationManager.AppSettings.ReadSetting("UseUserDequeue");
                    if (bool.Parse(enabledUserColumns))
                    {
                        _userData = true;
                    }
                    //Create the queue if it doesn't exist
                    if (!createQueue.QueueExists)
                    {
                        //queue options
                        createQueue.Options.EnableDelayedProcessing = true;
                        createQueue.Options.EnableHeartBeat = true;
                        createQueue.Options.EnableMessageExpiration = true;
                        createQueue.Options.EnableStatus = true;
                        createQueue.Options.EnableStatusTable = true;

                        if (!string.IsNullOrEmpty(enabledUserColumns) && bool.Parse(enabledUserColumns))
                        {
                            createQueue.Options.AdditionalColumnsOnMetaData = true;
                            createQueue.Options.AdditionalColumns.Add(new Column("DayOfWeek", ColumnTypes.Int, true, null));
                        }

                        var result = createQueue.CreateQueue();
                        log.Information(result.Status.ToString());
                    }
                    else log.Warning("Queue already exists; not creating; note that any setting changes won't be applied");
                }
            }

            //create the producer
            using (var queueContainer = new QueueContainer<SqlServerMessageQueueInit>(serviceRegister =>
                Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLServerProducer", serviceRegister)
                , options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
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
        /// Creates an expired message by having it expire 1 second in the future and delaying processing for 5 seconds
        /// </summary>
        /// <returns></returns>
        private static IAdditionalMessageData ExpiredData()
        {
            var data = new AdditionalMessageData();
            data.SetExpiration(TimeSpan.FromSeconds(1));
            data.SetDelay(TimeSpan.FromSeconds(5));
            if (_userData)
            {
                data.AdditionalMetaData.Add(new AdditionalMetaData<int>("DayOfWeek", Convert.ToInt32(DateTime.Today.DayOfWeek)));
            }
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
            if (_userData)
            {
                data.AdditionalMetaData.Add(new AdditionalMetaData<int>("DayOfWeek", Convert.ToInt32(DateTime.Today.DayOfWeek)));
            }
            return data;
        }

        private static IAdditionalMessageData DelayedProcessing(int seconds)
        {
            var data = new AdditionalMessageData();
            data.SetDelay(TimeSpan.FromSeconds(seconds));
            data.SetExpiration(TimeSpan.FromDays(1));
            if (_userData)
            {
                data.AdditionalMetaData.Add(new AdditionalMetaData<int>("DayOfWeek", Convert.ToInt32(DateTime.Today.DayOfWeek)));
            }
            return data;
        }
    }
}
