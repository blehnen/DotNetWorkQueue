using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.IntegrationTests.Shared.Producer;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Basic
{
    /// <summary>
    /// A message that is rolled back goes to the back of the queue rather than straight back to the front.
    ///
    /// With delayed processing on, the de-queue orders by QueueProcessTime, and the rollback is what moves
    /// that value forward. A rollback that leaves it alone leaves the message exactly where it was - first
    /// in line - so a consumer that keeps rejecting the same message re-reads it forever and never reaches
    /// the messages behind it. One message is then enough to starve a whole queue.
    ///
    /// The rollback builds its statement from two flags, and the one that asks for the QueueProcessTime
    /// column was being passed false on the branch that also carries a heartbeat. The parameter was still
    /// bound, so nothing failed and nothing was logged - the column simply was not in the update. That
    /// branch used to be reached only after a worker's first beat had landed; since the de-queue started
    /// publishing its claim (GitHub #336) every rollback reaches it, which turned a rare case into the
    /// only case (GitHub #352).
    /// </summary>
    [TestClass]
    [Retry(1)]
    public class RolledBackMessageOrderingTests
    {
        private const int MessageCount = 3;

        [TestMethod]
        public void AConsumerThatRejectsEveryMessage_StillReachesThemAll()
        {
            var queueName = GenerateQueueName.Create();
            var connectionString = ConnectionInfo.ConnectionString;
            var queueConnection = new QueueConnection(queueName, connectionString);
            var logProvider = LoggerShared.Create(queueName, GetType().Name);

            using (var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>(
                serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
            {
                var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);
                try
                {
                    //delayed processing is what puts QueueProcessTime in the order by; the heartbeat and
                    //status options are what make the de-queue stamp a claim, which is the branch at issue
                    oCreation.Options.EnableDelayedProcessing = true;
                    oCreation.Options.EnableHeartBeat = true;
                    oCreation.Options.EnableStatus = true;
                    Assert.IsTrue(oCreation.CreateQueue().Success);

                    new ProducerShared().RunTest<SqlServerMessageQueueInit, FakeMessage>(queueConnection, false,
                        MessageCount, logProvider, Helpers.GenerateData, Helpers.Verify, false, oCreation.Scope, false);

                    var delivered = new ConcurrentQueue<long>();
                    var enough = new ManualResetEventSlim(false);

                    using (var container = new QueueContainer<SqlServerMessageQueueInit>(
                        serviceRegister => serviceRegister.Register(() => logProvider, LifeStyles.Singleton)))
                    using (var queue = container.CreateConsumer(queueConnection))
                    {
                        //one worker, so the order messages arrive in is the order the de-queue chose rather
                        //than whichever worker happened to be free
                        queue.Configuration.Worker.WorkerCount = 1;
                        queue.Configuration.Worker.TimeToWaitForWorkersToStop = TimeSpan.FromSeconds(5);
                        queue.Configuration.Worker.SingleWorkerWhenNoWorkFound = true;
                        queue.Configuration.HeartBeat.UpdateTime = TimeSpan.FromSeconds(120);
                        queue.Configuration.HeartBeat.Time = TimeSpan.FromSeconds(360);
                        queue.Configuration.HeartBeat.MonitorTime = TimeSpan.FromSeconds(365);

                        queue.Start<FakeMessage>((message, notifications) =>
                        {
                            delivered.Enqueue((long)message.MessageId.Id.Value);
                            if (delivered.Count >= MessageCount)
                                enough.Set();

                            //cancelling is what rolls the message back; an ordinary exception would send it
                            //down the error path instead, which is a different statement
                            throw new OperationCanceledException("rejected on purpose");
                        }, CreateNotifications.Create(logProvider));

                        Assert.IsTrue(enough.Wait(TimeSpan.FromSeconds(60)),
                            $"only {delivered.Count} deliveries happened in 60 seconds");
                    }

                    var first = delivered.Take(MessageCount).ToList();
                    Assert.AreEqual(MessageCount, first.Distinct().Count(),
                        "the same message was handed out again before the others were reached, so the rollback "
                        + $"left it at the front of the queue. Deliveries: {string.Join(", ", first)}");
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
