using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using SampleShared;
using Serilog;

namespace SQLiteSchedulerConsumer
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
            var fileLocation = Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), "Documents");
            var queueName = ConfigurationManager.AppSettings.ReadSetting("QueueName");
            var connectionString =
                $"Data Source={fileLocation}{ConfigurationManager.AppSettings.ReadSetting("Database")};Version=3;";
            var queueConnection = new QueueConnection(queueName, connectionString);
            using (var createQueueContainer = new QueueCreationContainer<SqLiteMessageQueueInit>(serviceRegister =>
                Injectors.AddInjectors(log, SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLiteSchedulerConsumer", serviceRegister), 
                options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var createQueue =
                    createQueueContainer.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection))
                {
                    if (!createQueue.QueueExists)
                    {
                        //the consumer can't do anything if the queue hasn't been created
                        Log.Error($"Could not find {connectionString}. Verify that you have run the producer, which will create the queue");
                        return;
                    }
                }
            }

            using (var schedulerContainer = new SchedulerContainer(serviceRegister =>
                Injectors.AddInjectors(log, SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLiteSchedulerConsumer", serviceRegister), 
                options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var scheduler = schedulerContainer.CreateTaskScheduler())
                {
                    var factory = schedulerContainer.CreateTaskFactory(scheduler);
                    factory.Scheduler.Configuration.MaximumThreads = 8; //8 background threads
                    factory.Scheduler.Configuration.MaxQueueSize = 1; //allow work to be de-queued but held in memory until a thread is free

                    //note - the same factory can be passed to multiple queue instances - don't dispose the scheduler container until all queues have finished
                    factory.Scheduler.Start(); //the scheduler must be started before passing it to a queue

                    using (var queueContainer = new QueueContainer<SqLiteMessageQueueInit>(serviceRegister =>
                        Injectors.AddInjectors(log, SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "SQLiteSchedulerConsumer", serviceRegister), 
                        options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
                    {
                        using (var queue = queueContainer.CreateConsumerMethodQueueScheduler(queueConnection, factory))
                        {
                            //set some processing options and start looking for work
                            //in the async model, the worker count is how many threads are querying the queue - the scheduler runs the work
                            queue.Configuration.Worker.WorkerCount = 1; //lets just run 1 thread that queries the database

                            queue.Configuration.HeartBeat.UpdateTime = "sec(*%10)"; //set a heartbeat every 10 seconds
                            queue.Configuration.HeartBeat.MonitorTime =
                                TimeSpan.FromSeconds(15); //check for dead records every 15 seconds
                            queue.Configuration.HeartBeat.Time =
                                TimeSpan.FromSeconds(
                                    35); //records with no heartbeat after 35 seconds are considered dead

                            //an invalid data exception will be re-tried 3 times, with delays of 3, 6 and then finally 9 seconds
                            queue.Configuration.TransportConfiguration.RetryDelayBehavior.Add(
                                typeof(InvalidDataException),
                                new List<TimeSpan>
                                    {TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(9)});

                            queue.Configuration.MessageExpiration.Enabled = true;
                            queue.Configuration.MessageExpiration.MonitorTime =
                                TimeSpan.FromSeconds(20); //check for expired messages every 20 seconds
                            queue.Start(); //when running linq statements, there is no message handler, as the producer tells us what to run
                            Console.WriteLine("Processing messages - press any key to stop");
                            Console.ReadKey((true));
                        }
                    }
                }
            }

            //if jaeger is using udp, sometimes the messages get lost; there doesn't seem to be a flush() call ?
            if (SharedConfiguration.EnableTrace)
                System.Threading.Thread.Sleep(2000);
        }
    }
}
