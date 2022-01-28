using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SqlServer;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using SampleShared;
using Serilog;

namespace SQLServerConsumer
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

            //verify that the queue exists
            var queueName = ConfigurationManager.AppSettings.ReadSetting("QueueName");
            var connectionString = ConfigurationManager.AppSettings.ReadSetting("Database");
            var queueConnection = new QueueConnection(queueName, connectionString);

            using (var createQueueContainer = new QueueCreationContainer<SqlServerMessageQueueInit>(serviceRegister =>
                Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLServerConsumer", serviceRegister)
                , options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var createQueue =
                    createQueueContainer.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection))
                {
                    if (!createQueue.QueueExists)
                    {
                        //the consumer can't do anything if the queue hasn't been created
                        Log.Error($"Could not find {connectionString}. Verify that you have run the producer, which will create the queue");
                        return;
                    }
                }
            }

            using (var queueContainer = new QueueContainer<SqlServerMessageQueueInit>(serviceRegister =>
                Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLServerConsumer", serviceRegister)
                , options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var queue = queueContainer.CreateConsumer(queueConnection))
                {
                    //set some processing options and start looking for work
                    queue.Configuration.Worker.WorkerCount = 4; //lets run 4 worker threads
                    queue.Configuration.HeartBeat.UpdateTime = "sec(*%10)"; //set a heartbeat every 10 seconds
                    queue.Configuration.HeartBeat.MonitorTime = TimeSpan.FromSeconds(15); //check for dead records every 15 seconds
                    queue.Configuration.HeartBeat.Time = TimeSpan.FromSeconds(35); //records with no heartbeat after 35 seconds are considered dead

                    //an invalid data exception will be re-tried 3 times, with delays of 3, 6 and then finally 9 seconds
                    queue.Configuration.TransportConfiguration.RetryDelayBehavior.Add(typeof(InvalidDataException), new List<TimeSpan> { TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(9) });

                    queue.Configuration.MessageExpiration.Enabled = true;
                    queue.Configuration.MessageExpiration.MonitorTime = TimeSpan.FromSeconds(20); //check for expired messages every 20 seconds

                    var enabledUserColumns = ConfigurationManager.AppSettings.ReadSetting("UseUserDequeue");
                    if (!string.IsNullOrEmpty(enabledUserColumns) && bool.Parse(enabledUserColumns))
                    {
                        var dayofWeek = int.Parse(ConfigurationManager.AppSettings.ReadSetting("UserDayOfWeek"));
                        log.Information( $"Only processing items created on {((DayOfWeek)dayofWeek).ToString()}");
                        queue.Configuration.SetUserParametersAndClause(() => Parameters(dayofWeek), WhereClause);
                    }

                    queue.Start<SimpleMessage>(MessageProcessing.HandleMessages);
                    Console.WriteLine("Processing messages - press any key to stop");
                    Console.ReadKey((true));
                }
            }

            //if jaeger is using udp, sometimes the messages get lost; there doesn't seem to be a flush() call ?
            if (SharedConfiguration.EnableTrace)
                System.Threading.Thread.Sleep(2000);
        }

        private static string WhereClause()
        {
            return "(DayOfWeek = @DayOfWeek)";
        }

        private static List<SqlParameter> Parameters(int dayOfWeek)
        {
            var list = new List<SqlParameter>();
            var userParam = new SqlParameter("@DayOfWeek", SqlDbType.Int)
            {
                Value = dayOfWeek
            };
            list.Add(userParam);
            return list;
        }
    }
}
