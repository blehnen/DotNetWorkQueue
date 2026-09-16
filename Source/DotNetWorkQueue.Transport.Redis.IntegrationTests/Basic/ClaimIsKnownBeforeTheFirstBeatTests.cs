using System;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Basic
{
    /// <summary>
    /// A worker knows the heartbeat its claim is held by from the moment it is handed the message.
    ///
    /// The de-queue stamps a heartbeat and every later beat replaces it, and both places that prove
    /// ownership work by naming the current value. Until #336 the worker only learned the value when its
    /// own first beat landed, so in between it held a claim it could not name: the beat gave the claim up
    /// rather than renewing it, and the rollback stopped checking whose row it was reset.
    ///
    /// The value reaches user code through <see cref="IWorkerNotification.HeartBeat"/>, so the handler is
    /// where it can be observed without reaching into the library.
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class ClaimIsKnownBeforeTheFirstBeatTests
    {
        [TestMethod]
        public void TheClaimIsKnownWhenTheHandlerRuns()
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var queueConnection = new QueueConnection(queueName, connectionString);
            var logProvider = LoggerShared.Create(queueName, GetType().Name);

            using (var queueCreator = new QueueCreationContainer<RedisQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<RedisQueueCreation>(queueConnection);
                try
                {
                    //Redis has no schema options to turn on; the working set score is the claim and it is
                    //always written by the de-queue
                    Assert.IsTrue(oCreation.CreateQueue().Success);

                    new ProducerShared().RunTest<RedisQueueInit, FakeMessage>(queueConnection, false, 1,
                        logProvider, Helpers.GenerateData, Helpers.Verify, false, oCreation.Scope, false);

                    DateTime? claimWhenHandlerRan = null;
                    var handlerRan = new ManualResetEventSlim(false);

                    using (var container = new QueueContainer<RedisQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                    using (var queue = container.CreateConsumer(queueConnection))
                    {
                        //The update time has to exceed the whole wait below, not merely the handler. A beat
                        //of this worker's own would populate the same status, so if one could land before
                        //delivery the test would pass without the de-queue's value ever being read - it
                        //would be measuring the thing it is supposed to rule out.
                        //Time is three times UpdateTime because the consumer refuses to start otherwise.
                        queue.Configuration.Worker.WorkerCount = 1;
                        queue.Configuration.Worker.TimeToWaitForWorkersToStop = TimeSpan.FromSeconds(5);
                        queue.Configuration.Worker.SingleWorkerWhenNoWorkFound = true;
                        queue.Configuration.HeartBeat.UpdateTime = TimeSpan.FromSeconds(120);
                        queue.Configuration.HeartBeat.Time = TimeSpan.FromSeconds(360);
                        queue.Configuration.HeartBeat.MonitorTime = TimeSpan.FromSeconds(365);

                        queue.Start<FakeMessage>((message, notifications) =>
                        {
                            claimWhenHandlerRan = notifications.HeartBeat?.Status?.LastHeartBeatTime;
                            handlerRan.Set();
                        }, CreateNotifications.Create(logProvider));

                        Assert.IsTrue(handlerRan.Wait(TimeSpan.FromSeconds(60)), "the message was never delivered");
                    }

                    Assert.IsNotNull(claimWhenHandlerRan,
                        "the worker did not know the heartbeat its claim is held by, so neither the beat nor the "
                        + "rollback could name it");

                    //and it is the de-queue's stamp rather than a placeholder: the queue was created moments
                    //ago, so the claim has to sit between then and now
                    Assert.IsGreaterThan(DateTime.UtcNow.AddMinutes(-5), claimWhenHandlerRan.Value.ToUniversalTime(),
                        "the claim is older than this test run, so it is not the stamp the de-queue wrote");
                    Assert.IsLessThan(DateTime.UtcNow.AddMinutes(5), claimWhenHandlerRan.Value.ToUniversalTime(),
                        "the claim is in the future");
                }
                finally
                {
                    oCreation.RemoveQueue();
                    oCreation.Dispose();
                }
            }
        }
    }
}
