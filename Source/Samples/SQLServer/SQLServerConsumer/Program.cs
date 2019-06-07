using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using DotNetWorkQueue;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using SampleShared;
using Serilog;

namespace SQLServerConsumer
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

            //verify that the queue exists
            var queueName = ConfigurationManager.AppSettings.ReadSetting("QueueName");
            var connectionString = ConfigurationManager.AppSettings.ReadSetting("Database");

            using (var createQueueContainer = new QueueCreationContainer<SqlServerMessageQueueInit>(serviceRegister =>
                Injectors.AddInjectors(log, SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLServerConsumer", serviceRegister)
                , options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var createQueue =
                    createQueueContainer.GetQueueCreation<SqlServerMessageQueueCreation>(queueName, connectionString))
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
                Injectors.AddInjectors(log, SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLServerConsumer", serviceRegister)
                , options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var queue = queueContainer.CreateConsumer(queueName, connectionString))
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
                    queue.Start<SimpleMessage>(MessageProcessing.HandleMessages);
                    Console.WriteLine("Processing messages - press any key to stop");
                    Console.ReadKey((true));
                }
            }

            //if jaeger is using udp, sometimes the messages get lost; there doesn't seem to be a flush() call ?
            if (SharedConfiguration.EnableTrace)
                System.Threading.Thread.Sleep(2000);
        }
    }
}
