// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Transport.Memory
{
    /// <summary>
    /// The in-memory store's de-queue, driven directly.
    /// </summary>
    [TestClass]
    public class DataStorageTests
    {
        [TestMethod]
        public void A_Message_That_Cannot_Be_Reassembled_Is_Poison()
        {
            //the de-queue has already taken the message off the queue by the time the body is
            //turned back into a message, so a failure there cannot be retried by putting it back -
            //it is reported as poison instead. Nothing covered this path.
            var messageFactory = Substitute.For<IMessageFactory>();
            messageFactory
                .When(x => x.Create(Arg.Any<object>(), Arg.Any<IDictionary<string, object>>()))
                .Do(_ => throw new InvalidOperationException("cannot re-assemble"));

            using var test = Create(messageFactory);
            var id = test.SendMessage(Message(), MessageData());
            Assert.AreNotEqual(Guid.Empty, id);

            var thrown = Assert.ThrowsExactly<PoisonMessageException>(
                () => test.GetNextMessage(null, TimeSpan.FromMilliseconds(50)));

            Assert.AreEqual(id, (Guid)thrown.MessageId.Id.Value);
        }

        [TestMethod]
        public void Disposing_After_A_De_Queue_Does_Not_Throw()
        {
            //the de-queue builds a cancellation source that lives as long as this object, so
            //disposal has to cope with it having been created
            var test = Create(MessageFactory());
            test.SendMessage(Message(), MessageData());
            Assert.IsNotNull(test.GetNextMessage(null, TimeSpan.FromMilliseconds(50)));

            test.Dispose();
            test.Dispose(); //disposal is guarded, so a second call is a no-op rather than a throw
        }

        [TestMethod]
        public void Disposing_Without_A_De_Queue_Does_Not_Throw()
        {
            //the cancellation source is never built in this case, so disposal must not force it
            var test = Create(MessageFactory());
            test.Dispose();
        }

        [TestMethod]
        public void A_Failed_Message_Leaves_The_Working_Set()
        {
            //the error path clears the message id after the move, so nothing deletes the message
            //later; before #439 every failed message stayed in the working set for good
            using var test = Create(MessageFactory());
            var id = test.SendMessage(Message(), MessageData());
            Assert.IsNotNull(test.GetNextMessage(null, TimeSpan.FromMilliseconds(50)));
            Assert.AreEqual(1, test.WorkingRecordCount);

            test.MoveToErrorQueue(new IndexOutOfRangeException(), id, Substitute.For<IMessageContext>());

            Assert.AreEqual(0, test.WorkingRecordCount);
            Assert.AreEqual(1, test.GetErrorCount());
            Assert.IsNull(test.FindMessage(id, out _));
        }

        [TestMethod]
        public void A_Job_Whose_Message_Failed_Can_Be_Queued_Again()
        {
            //the job name used to keep pointing at the failed message, so its next occurrence was
            //reported as still processing and the scheduler never queued the job again
            var firstRun = new DateTimeOffset(2001, 2, 3, 4, 0, 0, TimeSpan.Zero);
            var nextRun = firstRun.AddMinutes(1);
            using var test = Create(MessageFactory(), "job", firstRun, nextRun);
            var id = test.SendMessage(Message(), MessageData());
            Assert.IsNotNull(test.GetNextMessage(null, TimeSpan.FromMilliseconds(50)));
            Assert.AreEqual(QueueStatuses.Processing, test.DoesJobExist("job", nextRun));

            test.MoveToErrorQueue(new IndexOutOfRangeException(), id, Substitute.For<IMessageContext>());

            Assert.AreEqual(QueueStatuses.NotQueued, test.DoesJobExist("job", nextRun));
            Assert.AreNotEqual(Guid.Empty, test.SendMessage(Message(), MessageData()));
            Assert.AreEqual(QueueStatuses.Waiting, test.DoesJobExist("job", nextRun));
        }

        [TestMethod]
        public void A_Failed_Message_Does_Not_Remove_A_Newer_Message_For_The_Same_Job()
        {
            //the job entry is removed only while it still points at the failed message: once the job
            //has been deleted and queued again, the name belongs to the new message
            var firstRun = new DateTimeOffset(2001, 2, 3, 4, 0, 0, TimeSpan.Zero);
            var nextRun = firstRun.AddMinutes(1);
            using var test = Create(MessageFactory(), "job", firstRun, nextRun);
            var failed = test.SendMessage(Message(), MessageData());
            Assert.IsNotNull(test.GetNextMessage(null, TimeSpan.FromMilliseconds(50)));
            test.DeleteJob("job");
            Assert.AreNotEqual(Guid.Empty, test.SendMessage(Message(), MessageData()));

            test.MoveToErrorQueue(new IndexOutOfRangeException(), failed, Substitute.For<IMessageContext>());

            Assert.AreEqual(QueueStatuses.Waiting, test.DoesJobExist("job", nextRun));
        }

        [TestMethod]
        public void Deleting_A_Message_Does_Not_Remove_A_Newer_Message_For_The_Same_Job()
        {
            //DeleteMessage used to remove the job name's entry whichever message it pointed at
            var firstRun = new DateTimeOffset(2001, 2, 3, 4, 0, 0, TimeSpan.Zero);
            var nextRun = firstRun.AddMinutes(1);
            using var test = Create(MessageFactory(), "job", firstRun, nextRun);
            var old = test.SendMessage(Message(), MessageData());
            Assert.IsNotNull(test.GetNextMessage(null, TimeSpan.FromMilliseconds(50)));
            test.DeleteJob("job");
            Assert.AreNotEqual(Guid.Empty, test.SendMessage(Message(), MessageData()));

            Assert.IsTrue(test.DeleteMessage(old));

            Assert.AreEqual(QueueStatuses.Waiting, test.DoesJobExist("job", nextRun));
        }

        [TestMethod]
        public void A_Job_Whose_Message_Was_Poison_Can_Be_Queued_Again()
        {
            //a poison message never reaches the working set, so its job name has to be dropped where
            //the poison is found; otherwise the next occurrence queued but could not register
            var messageFactory = Substitute.For<IMessageFactory>();
            messageFactory
                .When(x => x.Create(Arg.Any<object>(), Arg.Any<IDictionary<string, object>>()))
                .Do(_ => throw new InvalidOperationException("cannot re-assemble"));
            var firstRun = new DateTimeOffset(2001, 2, 3, 4, 0, 0, TimeSpan.Zero);
            var nextRun = firstRun.AddMinutes(1);
            using var test = Create(messageFactory, "job", firstRun, nextRun);
            var id = test.SendMessage(Message(), MessageData());
            Assert.ThrowsExactly<PoisonMessageException>(
                () => test.GetNextMessage(null, TimeSpan.FromMilliseconds(50)));
            //what the poison handler does next
            test.MoveToErrorQueue(new InvalidOperationException(), id, Substitute.For<IMessageContext>());

            Assert.AreNotEqual(Guid.Empty, test.SendMessage(Message(), MessageData()));

            Assert.AreEqual(QueueStatuses.Waiting, test.DoesJobExist("job", nextRun));
        }

        [TestMethod]
        public void Concurrent_Sends_Of_One_Job_Occurrence_Queue_Exactly_One()
        {
            //the check and the registration were two steps, so senders released together could all
            //pass the check; every one was queued, and all but one were missing from the job index
            var occurrence = new DateTimeOffset(2001, 2, 3, 4, 0, 0, TimeSpan.Zero);
            const int senders = 8;
            for (var round = 0; round < 50; round++)
            {
                using var test = Create(MessageFactory(), "job", Enumerable.Repeat(occurrence, senders).ToArray());
                var queued = 0;
                var refused = 0;
                using var barrier = new Barrier(senders);
                var threads = Enumerable.Range(0, senders).Select(_ => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    try
                    {
                        if (test.SendMessage(Message(), MessageData()) != Guid.Empty)
                            Interlocked.Increment(ref queued);
                    }
                    catch (DotNetWorkQueueException error) when (error.Message.Contains(
                                 "Failed to insert record - the job has already been queued or processed"))
                    {
                        Interlocked.Increment(ref refused);
                    }
                })).ToList();
                threads.ForEach(t => t.Start());
                threads.ForEach(t => t.Join());

                Assert.AreEqual(1, queued, $"round {round}: queued");
                Assert.AreEqual(senders - 1, refused, $"round {round}: refused");
                Assert.AreEqual(1, test.RecordCount, $"round {round}: records");
                Assert.AreEqual(QueueStatuses.Waiting, test.DoesJobExist("job", occurrence), $"round {round}: index");
            }
        }

        [TestMethod]
        public void A_Job_Entry_Whose_Message_Is_Gone_Does_Not_Block_The_Job()
        {
            //no public call leaves such an entry since #439 and #441, so plant one; it must be replaced,
            //not treated as a duplicate, or the job could never be queued again
            var occurrence = new DateTimeOffset(2001, 2, 3, 4, 0, 0, TimeSpan.Zero);
            using var test = Create(MessageFactory(), "job", occurrence);
            var jobsField = typeof(DataStorage).GetField("Jobs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var connectionField = typeof(DataStorage).GetField("_connectionInformation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var allJobs = (System.Collections.Concurrent.ConcurrentDictionary<IConnectionInformation, System.Collections.Concurrent.ConcurrentDictionary<string, Guid>>)jobsField.GetValue(null);
            allJobs[(IConnectionInformation)connectionField.GetValue(test)]["job"] = Guid.NewGuid();

            var id = test.SendMessage(Message(), MessageData());

            Assert.AreNotEqual(Guid.Empty, id);
            Assert.AreEqual(QueueStatuses.Waiting, test.DoesJobExist("job", occurrence));
        }

        /// <summary>A date far from any machine clock, so a timestamp from the wrong source cannot match.</summary>
        private static readonly DateTime ProviderNow = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);

        private static DataStorage Create(IMessageFactory messageFactory, string jobName = "",
            params DateTimeOffset[] scheduledTimes)
        {
            var jobSchedulerMetaData = Substitute.For<IJobSchedulerMetaData>();
            jobSchedulerMetaData.GetJobName(Arg.Any<IAdditionalMessageData>()).Returns(jobName);
            if (scheduledTimes.Length > 0)
            {
                //one per send. The event time is when the scheduler actually sent it, which is never
                //exactly the scheduled time
                var eventTimes = Array.ConvertAll(scheduledTimes, t => t.AddSeconds(5));
                jobSchedulerMetaData.GetScheduledTime(Arg.Any<IAdditionalMessageData>())
                    .Returns(scheduledTimes[0], scheduledTimes[1..]);
                jobSchedulerMetaData.GetEventTime(Arg.Any<IAdditionalMessageData>())
                    .Returns(eventTimes[0], eventTimes[1..]);
            }

            //a queue name of its own: the store is a process-wide static keyed by connection
            var connection = new DotNetWorkQueue.Transport.Memory.ConnectionInformation(
                new QueueConnection("dataStorageTests" + Guid.NewGuid().ToString("N"), "memory"));

            var time = Substitute.For<IGetTime>();
            time.GetCurrentUtcDate().Returns(ProviderNow);
            var timeFactory = Substitute.For<IGetTimeFactory>();
            timeFactory.Create().Returns(time);

            return new DataStorage(jobSchedulerMetaData, connection,
                Substitute.For<IReceivedMessageFactory>(), messageFactory,
                Substitute.For<IQueueCancelWork>(), timeFactory);
        }

        private static IMessageFactory MessageFactory()
        {
            var messageFactory = Substitute.For<IMessageFactory>();
            messageFactory.Create(Arg.Any<object>(), Arg.Any<IDictionary<string, object>>())
                .Returns(_ => Substitute.For<IMessage>());
            return messageFactory;
        }

        private static IMessage Message()
        {
            var message = Substitute.For<IMessage>();
            message.Body.Returns("body");
            message.Headers.Returns(new Dictionary<string, object>());
            return message;
        }

        private static IAdditionalMessageData MessageData()
        {
            var data = Substitute.For<IAdditionalMessageData>();
            data.CorrelationId.Returns(new MessageCorrelationId(Guid.NewGuid()));
            return data;
        }
    }
}
