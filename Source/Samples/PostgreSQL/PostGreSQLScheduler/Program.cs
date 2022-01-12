using DotNetWorkQueue;
using SampleShared;
using Serilog;
using System;
using System.Configuration;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;

namespace PostGreSQLScheduler
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
            using (var jobQueueCreation =
                new JobQueueCreationContainer<PostgreSqlMessageQueueInit>(serviceRegister =>
                    Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "PostgreSqlScheduler", serviceRegister)
                    , options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var createQueue =
                    jobQueueCreation.GetQueueCreation<PostgreSqlJobQueueCreation>(queueConnection))
                {

                    //queue options
                    createQueue.Options.EnableDelayedProcessing = true;
                    createQueue.Options.EnableHeartBeat = true;
                    createQueue.Options.EnableMessageExpiration = false;
                    createQueue.Options.EnableStatus = true;
                    createQueue.Options.EnableStatusTable = true;
                    var result = createQueue.CreateJobSchedulerQueue(serviceRegister =>
                        Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "PostgreSqlScheduler", serviceRegister), queueConnection,
                        options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos), false);
                    log.Information(result.Status.ToString());

                }
            }

            using (var jobContainer = new JobSchedulerContainer(serviceRegister =>
                Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "PostgreSqlScheduler", serviceRegister)
                , options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
            {
                using (var scheduler = jobContainer.CreateJobScheduler(serviceRegister =>
                    Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "PostgreSqlScheduler", serviceRegister),
                    serviceRegister =>
                        Injectors.AddInjectors(Helpers.CreateForSerilog(), SharedConfiguration.EnableTrace, SharedConfiguration.EnableMetrics, SharedConfiguration.EnableCompression, SharedConfiguration.EnableEncryption, "PostgreSqlScheduler", serviceRegister)
                    , options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)
                    , options => Injectors.SetOptions(options, SharedConfiguration.EnableChaos)))
                {
                    //start may be called before or after adding jobs
                    scheduler.Start();

                    var keepRunning = true;
                    IScheduledJob job1 = null;
                    IScheduledJob job2 = null;
                    IScheduledJob job3 = null;
                    while (keepRunning)
                    {
                        Console.WriteLine(@"a) Schedule job1
b) Schedule job2
c) Schedule job3

d) View scheduled jobs

e) Remove job1
f) Remove job2
g) Remove job3

q) Quit");
                        var key = char.ToLower(Console.ReadKey(true).KeyChar);

                        try
                        {
                            switch (key)
                            {
                                case 'a':
                                    job1 = scheduler.AddUpdateJob<PostgreSqlMessageQueueInit, PostgreSqlJobQueueCreation>("test job1",
                                        queueConnection,
                                        "sec(0,5,10,15,20,25,30,35,40,45,50,55)",
                                        (message, workerNotification) => Console.WriteLine("test job1 " + message.MessageId.Id.Value));
                                    log.Information("job scheduled");
                                    break;
                                case 'b':
                                    job2 = scheduler.AddUpdateJob<PostgreSqlMessageQueueInit, PostgreSqlJobQueueCreation>("test job2",
                                        queueConnection,
                                        "min(*)",
                                        (message, workerNotification) => Console.WriteLine("test job2 " + message.MessageId.Id.Value));
                                    log.Information("job scheduled");
                                    break;
                                case 'c':
                                    job3 = scheduler.AddUpdateJob<PostgreSqlMessageQueueInit, PostgreSqlJobQueueCreation>("test job3",
                                        queueConnection,
                                        "sec(30)",
                                        (message, workerNotification) => Console.WriteLine("test job3 " + message.MessageId.Id.Value));
                                    log.Information("job scheduled");
                                    break;
                                case 'd':
                                    var jobs = scheduler.GetAllJobs();
                                    foreach (var job in jobs)
                                    {
                                        Log.Information("Job: {@job}", job);
                                    }
                                    break;
                                case 'e':
                                    if (job1 != null)
                                    {
                                        job1.StopSchedule();
                                        if (scheduler.RemoveJob(job1.Name))
                                        {
                                            job1 = null;
                                            log.Information("job removed");
                                        }
                                    }
                                    break;
                                case 'f':
                                    if (job2 != null)
                                    {
                                        job2.StopSchedule();
                                        if (scheduler.RemoveJob(job2.Name))
                                        {
                                            job2 = null;
                                            log.Information("job removed");
                                        }
                                    }
                                    break;
                                case 'g':
                                    if (job3 != null)
                                    {
                                        job3.StopSchedule();
                                        if (scheduler.RemoveJob(job3.Name))
                                        {
                                            job3 = null;
                                            log.Information("job removed");
                                        }
                                    }
                                    break;
                                case 'q':
                                    Console.WriteLine("Quitting");
                                    keepRunning = false;
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(e, "Failed");
                        }
                    }
                }
            }
        }
    }
}
