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
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.JobScheduler;
using DotNetWorkQueue.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.JobScheduler
{
    /// <summary>
    /// Tests for the job scheduler's registration, removal, event relay and shutdown behavior.
    /// </summary>
    /// <remarks>
    /// The transport queue is reached only through a substituted <see cref="IJobQueue"/>, and time
    /// comes from a substituted <see cref="IGetTimeFactory"/>, so no real transport or clock is
    /// involved. Jobs are added with <c>autoRun: false</c> unless a test is specifically about the
    /// auto-start behavior — a running job cannot be removed.
    /// </remarks>
    [TestClass]
    public class JobSchedulerTests
    {
        private const string EveryMinute = "* * * * *";
        private static readonly DateTime UtcNow = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(10);

        private static void NoOpAction()
        {
        }

        private static readonly Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> Action =
            (message, notification) => NoOpAction();

        private sealed class Harness
        {
            public DotNetWorkQueue.JobScheduler.JobScheduler Scheduler { get; init; }
            public IProducerMethodJobQueue Queue { get; init; }
        }

        private static Harness CreateScheduler()
        {
            var queue = Substitute.For<IProducerMethodJobQueue>();
            queue.LastKnownEvent.Returns(Substitute.For<IJobSchedulerLastKnownEvent>());
            queue.Logger.Returns(Substitute.For<ILogger>());

            var jobQueue = Substitute.For<IJobQueue>();
            jobQueue.Get<NoOpTransport, NoOpJobQueueCreation>(Arg.Any<QueueConnection>(),
                Arg.Any<Action<QueueProducerConfiguration>>()).Returns(queue);
            jobQueue.Get<NoOpTransport>(Arg.Any<IJobQueueCreation>(), Arg.Any<QueueConnection>(),
                Arg.Any<Action<QueueProducerConfiguration>>()).Returns(queue);

            var time = Substitute.For<IGetTime>();
            time.GetCurrentUtcDate().Returns(UtcNow);
            var timeFactory = Substitute.For<IGetTimeFactory>();
            timeFactory.Create().Returns(time);

            return new Harness
            {
                Scheduler = new DotNetWorkQueue.JobScheduler.JobScheduler(jobQueue, timeFactory,
                    Substitute.For<ILogger>()),
                Queue = queue
            };
        }

        private static IScheduledJob AddJob(Harness harness, string name, bool autoRun = false)
        {
            return harness.Scheduler.AddUpdateJob<NoOpTransport, NoOpJobQueueCreation>(name,
                new QueueConnection("aQueue", "aConnection"), EveryMinute, Action, autoRun: autoRun);
        }

        // --- adding jobs ---

        [TestMethod]
        public void AddUpdateJob_EmptySchedule_Throws()
        {
            var harness = CreateScheduler();

            Action act = () => harness.Scheduler.AddUpdateJob<NoOpTransport, NoOpJobQueueCreation>("aJob",
                new QueueConnection("aQueue", "aConnection"), string.Empty, Action);

            Assert.Throws<ArgumentException>(act);
        }

        [TestMethod]
        public void AddUpdateJob_NameLongerThan255_Throws()
        {
            var harness = CreateScheduler();

            Action act = () => harness.Scheduler.AddUpdateJob<NoOpTransport, NoOpJobQueueCreation>(
                new string('x', 256), new QueueConnection("aQueue", "aConnection"), EveryMinute, Action);

            Assert.Throws<ArgumentException>(act);
        }

        [TestMethod]
        public void AddUpdateJob_ValidJob_IsRegistered()
        {
            var harness = CreateScheduler();

            var job = AddJob(harness, "aJob");

            Assert.AreEqual("aJob", job.Name);
            Assert.IsFalse(job.IsScheduleRunning, "autoRun was false");
            CollectionAssert.AreEquivalent(new[] { "aJob" },
                harness.Scheduler.GetAllJobs().Select(x => x.Name).ToArray());
        }

        [TestMethod]
        public void AddUpdateJob_AutoRun_StartsTheSchedule()
        {
            var harness = CreateScheduler();

            var job = AddJob(harness, "aJob", autoRun: true);

            Assert.IsTrue(job.IsScheduleRunning);
        }

        [TestMethod]
        public void AddUpdateJob_ExistingName_ReplacesTheJob()
        {
            var harness = CreateScheduler();
            var original = AddJob(harness, "aJob");

            var replacement = AddJob(harness, "aJob");

            Assert.AreNotSame(original, replacement);
            Assert.AreEqual(1, harness.Scheduler.GetAllJobs().Count());
            Assert.IsFalse(original.IsAttached, "the replaced job is detached");
        }

        [TestMethod]
        public void AddUpdateJob_QueueCreationInstanceOverload_IsRegistered()
        {
            var harness = CreateScheduler();

            var job = harness.Scheduler.AddUpdateJob<NoOpTransport>(Substitute.For<IJobQueueCreation>(),
                "aJob", new QueueConnection("aQueue", "aConnection"), EveryMinute, Action, autoRun: false);

            Assert.AreEqual("aJob", job.Name);
            Assert.AreEqual(1, harness.Scheduler.GetAllJobs().Count());
        }

        [TestMethod]
        public void AddUpdateJob_InstanceOverloadWithEmptySchedule_Throws()
        {
            var harness = CreateScheduler();

            Action act = () => harness.Scheduler.AddUpdateJob<NoOpTransport>(
                Substitute.For<IJobQueueCreation>(), "aJob", new QueueConnection("aQueue", "aConnection"),
                string.Empty, Action);

            Assert.Throws<ArgumentException>(act);
        }

        // --- listing ---

        [TestMethod]
        public void GetAllJobs_NoJobs_IsEmpty()
        {
            Assert.IsEmpty(CreateScheduler().Scheduler.GetAllJobs());
        }

        [TestMethod]
        public void GetAllJobs_ReturnsEveryRegisteredJob()
        {
            var harness = CreateScheduler();
            AddJob(harness, "first");
            AddJob(harness, "second");

            CollectionAssert.AreEquivalent(new[] { "first", "second" },
                harness.Scheduler.GetAllJobs().Select(x => x.Name).ToArray());
        }

        // --- removal ---

        [TestMethod]
        public void RemoveJob_UnknownName_ReturnsFalse()
        {
            Assert.IsFalse(CreateScheduler().Scheduler.RemoveJob("nope"));
        }

        [TestMethod]
        public void RemoveJob_StoppedJob_RemovesAndDetachesIt()
        {
            var harness = CreateScheduler();
            var job = AddJob(harness, "aJob");

            Assert.IsTrue(harness.Scheduler.RemoveJob("aJob"));
            Assert.IsFalse(job.IsAttached);
            Assert.IsEmpty(harness.Scheduler.GetAllJobs());
        }

        [TestMethod]
        public void RemoveJob_RunningJob_Throws()
        {
            var harness = CreateScheduler();
            AddJob(harness, "aJob", autoRun: true);

            Action act = () => harness.Scheduler.RemoveJob("aJob");

            Assert.Throws<JobSchedulerException>(act);
        }

        // --- shutdown ---

        [TestMethod]
        public void IsShuttingDown_BeforeDispose_IsFalse()
        {
            var harness = CreateScheduler();

            Assert.IsFalse(harness.Scheduler.IsShuttingDown);
            Assert.IsFalse(harness.Scheduler.IsDisposed);
        }

        [TestMethod]
        public void Dispose_MarksShuttingDownAndDisposed()
        {
            var harness = CreateScheduler();

            harness.Scheduler.Dispose();

            Assert.IsTrue(harness.Scheduler.IsDisposed);
            Assert.IsTrue(harness.Scheduler.IsShuttingDown);
        }

        [TestMethod]
        public void Dispose_CalledTwice_IsIdempotent()
        {
            var harness = CreateScheduler();

            harness.Scheduler.Dispose();
            harness.Scheduler.Dispose();

            Assert.IsTrue(harness.Scheduler.IsDisposed);
        }

        [TestMethod]
        public void Dispose_StopsAndDetachesRunningJobs()
        {
            var harness = CreateScheduler();
            var job = AddJob(harness, "aJob", autoRun: true);

            harness.Scheduler.Dispose();

            Assert.IsFalse(job.IsScheduleRunning);
            Assert.IsFalse(job.IsAttached);
        }

        [TestMethod]
        public void AddUpdateJob_AfterDispose_Throws()
        {
            var harness = CreateScheduler();
            harness.Scheduler.Dispose();

            Action act = () => AddJob(harness, "aJob");

            Assert.Throws<JobSchedulerException>(act);
        }

        [TestMethod]
        public void RemoveJob_AfterDispose_Throws()
        {
            var harness = CreateScheduler();
            harness.Scheduler.Dispose();

            Action act = () => harness.Scheduler.RemoveJob("aJob");

            Assert.Throws<JobSchedulerException>(act);
        }

        [TestMethod]
        public void Start_ThenDispose_ShutsDownCleanly()
        {
            var harness = CreateScheduler();

            harness.Scheduler.Start();
            harness.Scheduler.Dispose();

            Assert.IsTrue(harness.Scheduler.IsShuttingDown);
        }

        // --- event relay ---

        [TestMethod]
        public async Task JobEnQueue_IsRelayedToSchedulerSubscribers()
        {
            var harness = CreateScheduler();
            var job = (ScheduledJob)AddJob(harness, "aJob", autoRun: true);

            var result = Substitute.For<IJobQueueOutputMessage>();
            result.Status.Returns(JobQueuedStatus.Success);
            harness.Queue.SendAsync(Arg.Any<IScheduledJob>(), Arg.Any<DateTimeOffset>(),
                    Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                    Arg.Any<bool>())
                .Returns(Task.FromResult(result));

            var relayed = new TaskCompletionSource<IScheduledJob>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            harness.Scheduler.OnJobQueue += (scheduledJob, _) => relayed.TrySetResult(scheduledJob);

            await job.RunPendingEventAsync(new PendingEvent(new DateTimeOffset(UtcNow), job, 0))
                .ConfigureAwait(false);

            var delivered = await relayed.Task.WaitAsync(EventTimeout).ConfigureAwait(false);
            Assert.AreEqual("aJob", delivered.Name);
        }

        [TestMethod]
        public async Task JobNonFatalFailure_IsRelayedToSchedulerSubscribers()
        {
            var harness = CreateScheduler();
            var job = (ScheduledJob)AddJob(harness, "aJob", autoRun: true);

            var result = Substitute.For<IJobQueueOutputMessage>();
            result.Status.Returns(JobQueuedStatus.AlreadyQueuedProcessing);
            harness.Queue.SendAsync(Arg.Any<IScheduledJob>(), Arg.Any<DateTimeOffset>(),
                    Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                    Arg.Any<bool>())
                .Returns(Task.FromResult(result));

            var relayed = new TaskCompletionSource<IScheduledJob>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            harness.Scheduler.OnJobNonFatalFailureQueue += (scheduledJob, _) => relayed.TrySetResult(scheduledJob);

            await job.RunPendingEventAsync(new PendingEvent(new DateTimeOffset(UtcNow), job, 0))
                .ConfigureAwait(false);

            var delivered = await relayed.Task.WaitAsync(EventTimeout).ConfigureAwait(false);
            Assert.AreEqual("aJob", delivered.Name);
        }

        [TestMethod]
        public async Task JobException_IsRelayedToSchedulerSubscribers()
        {
            var harness = CreateScheduler();
            var job = (ScheduledJob)AddJob(harness, "aJob", autoRun: true);

            var sendFailure = new InvalidOperationException("could not send");
            harness.Queue.When(x => x.SendAsync(Arg.Any<IScheduledJob>(), Arg.Any<DateTimeOffset>(),
                    Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                    Arg.Any<bool>()))
                .Do(_ => throw sendFailure);

            var relayed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
            harness.Scheduler.OnJobQueueException += (_, exception) => relayed.TrySetResult(exception);

            await job.RunPendingEventAsync(new PendingEvent(new DateTimeOffset(UtcNow), job, 0))
                .ConfigureAwait(false);

            Assert.AreSame(sendFailure, await relayed.Task.WaitAsync(EventTimeout).ConfigureAwait(false));
        }

        // --- pending events ---

        [TestMethod]
        public void AddPendingEvent_AfterShutdown_IsIgnored()
        {
            var harness = CreateScheduler();
            var job = (ScheduledJob)AddJob(harness, "aJob");
            harness.Scheduler.Dispose();

            // must not throw; the scheduler drops events once shutting down
            harness.Scheduler.AddPendingEvent(new PendingEvent(new DateTimeOffset(UtcNow), job, 0));

            Assert.IsTrue(harness.Scheduler.IsShuttingDown);
        }
    }
}
