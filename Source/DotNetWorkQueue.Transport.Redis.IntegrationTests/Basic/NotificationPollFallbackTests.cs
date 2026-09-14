using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests.Basic
{
    /// <summary>
    /// A reader that is never told about work still looks at the queue.
    ///
    /// Redis pub/sub is fire and forget: a notification published while the subscriber's connection is
    /// briefly down is not replayed when it reconnects. Before this was bounded, a reader that missed
    /// one waited until the queue shut down - and the message it happens to is the last one in a queue,
    /// because every other message has a following enqueue to publish again and wake the reader. The
    /// symptom is a queue that stops delivering until the consumer is restarted.
    ///
    /// These tests never publish anything, which is the same position a reader is in after a lost
    /// notification. An unbounded wait does not return here at all; it has to be timed out by the test
    /// harness, which is the failure this prevents.
    /// </summary>
    [TestClass]
    public class NotificationPollFallbackTests
    {
        private static readonly TimeSpan Fallback = TimeSpan.FromMilliseconds(500);

        [TestMethod]
        public void Wait_WithNoNotification_ReturnsAfterTheFallbackInsteadOfBlocking()
        {
            using (var connection = new RedisConnection(new BaseConnectionInformation(
                new QueueConnection(GenerateQueueName.Create(), ConnectionInfo.ConnectionString))))
            {
                var names = new RedisNames(new BaseConnectionInformation(
                    new QueueConnection(GenerateQueueName.Create(), ConnectionInfo.ConnectionString)));

                using (var sub = new RedisQueueWorkSub(connection, names, new QueueCancelWork(), Fallback))
                {
                    sub.Reset();

                    //run it on another thread and join with a bound, so an unbounded wait fails
                    //this test rather than hanging the suite until the harness gives up
                    var watch = Stopwatch.StartNew();
                    var wait = Task.Run(() => sub.Wait());
                    Assert.IsTrue(wait.Wait(TimeSpan.FromSeconds(15)),
                        "the wait never returned, so it is not bounded by the fallback interval");
                    var result = wait.Result;
                    watch.Stop();

                    //true sends the caller back to re-read the queue, which is the whole point: the
                    //reader gets there without a notification rather than waiting for one that is
                    //never coming
                    Assert.IsTrue(result, "the wait reported that it should stop looking for work");
                    Assert.IsLessThan(TimeSpan.FromSeconds(15), watch.Elapsed,
                        $"the wait took {watch.Elapsed}, so it is not bounded by the fallback interval");
                }
            }
        }

        [TestMethod]
        public void WaitAsync_WithNoNotification_ReturnsAfterTheFallbackInsteadOfBlocking()
        {
            using (var connection = new RedisConnection(new BaseConnectionInformation(
                new QueueConnection(GenerateQueueName.Create(), ConnectionInfo.ConnectionString))))
            {
                var names = new RedisNames(new BaseConnectionInformation(
                    new QueueConnection(GenerateQueueName.Create(), ConnectionInfo.ConnectionString)));

                using (var sub = new RedisQueueWorkSub(connection, names, new QueueCancelWork(), Fallback))
                {
                    sub.Reset();

                    var watch = Stopwatch.StartNew();
                    var wait = sub.WaitAsync(CancellationToken.None).AsTask();
                    Assert.IsTrue(wait.Wait(TimeSpan.FromSeconds(15)),
                        "the wait never returned, so it is not bounded by the fallback interval");
                    var result = wait.Result;
                    watch.Stop();

                    Assert.IsTrue(result, "the wait reported that it should stop looking for work");
                    Assert.IsLessThan(TimeSpan.FromSeconds(15), watch.Elapsed,
                        $"the wait took {watch.Elapsed}, so it is not bounded by the fallback interval");
                }
            }
        }
    }
}
