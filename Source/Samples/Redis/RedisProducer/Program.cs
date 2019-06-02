using System;
using System.Configuration;
using DotNetWorkQueue;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis;
using DotNetWorkQueue.Transport.Redis.Basic;
using SampleShared;
using Serilog;

namespace RedisProducer
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
            
            //create the producer
            using (var queueContainer = new QueueContainer<RedisQueueInit>(serviceRegister =>
                Injectors.AddInjectors(log, SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "PostgreSqlProducer", serviceRegister)))
            {
                using (var queue = queueContainer.CreateProducer<SimpleMessage>(queueName, connectionString))
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
