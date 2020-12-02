using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Redis.Basic;
using SampleShared;
using Serilog;

namespace RedisConsumer
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
            var queueConnection = new QueueConnection(queueName, connectionString);
            using (var queueContainer = new QueueContainer<RedisQueueInit>(serviceRegister =>
                Injectors.AddInjectors(log, SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "RedisConsumer", serviceRegister)
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
