using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis;
using DotNetWorkQueue.Transport.Redis.Basic;
using SampleShared;
using Serilog;

namespace RedisProducerLinq
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

            var queueName = ConfigurationManager.AppSettings.ReadSetting("QueueName");
            var connectionString = ConfigurationManager.AppSettings.ReadSetting("Database");
            var queueConnection = new QueueConnection(queueName, connectionString);
            //create the producer
            using (var queueContainer = new QueueContainer<RedisQueueInit>(serviceRegister =>
                Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "RedisProducer", serviceRegister)
                , options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var queue = queueContainer.CreateMethodProducer(queueConnection))
                {
                    using (var admin = queueContainer.CreateAdminApi())
                    {
                        admin.AddQueueConnection(queueContainer, queueConnection);
                        RunProducer.RunLoop(queue, ExpiredData, ExpiredDataFuture, admin);
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
