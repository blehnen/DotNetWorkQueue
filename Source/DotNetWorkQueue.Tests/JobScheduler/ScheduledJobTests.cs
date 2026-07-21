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
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.JobScheduler;
using DotNetWorkQueue.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.JobScheduler
{
    /// <summary>
    /// Tests for a single scheduled job's lifecycle and event dispatch.
    /// </summary>
    /// <remarks>
    /// Time is supplied through a substituted <see cref="IGetTime"/> and the schedule through a
    /// substituted <see cref="IJobSchedule"/>, so every case is deterministic. The job's
    /// <c>Raise*</c> methods dispatch on the thread pool, so those tests await a
    /// <see cref="TaskCompletionSource{TResult}"/> signalled by the event handler — the wait ends
    /// the moment the event fires; the timeout only trips on a genuine failure.
    /// </remarks>
    [TestClass]
    public class ScheduledJobTests
    {
        private static readonly DateTime UtcNow = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(10);

        /// <summary>Target of the job's action expression; never actually invoked by these tests.</summary>
        private static void NoOpAction()
        {
        }

        private sealed class Harness
        {
            public IProducerMethodJobQueue Queue { get; init; }
            public IJobSchedule Schedule { get; init; }
            public IJobSchedulerLastKnownEvent LastKnownEvent { get; init; }
            public ScheduledJob Job { get; init; }
        }

        private static Harness CreateJob(TimeSpan window = default, bool attached = true)
        {
            // fully qualified: inside this namespace the bare name binds to the namespace, not the type
            var scheduler = new DotNetWorkQueue.JobScheduler.JobScheduler(Substitute.For<IJobQueue>(),
                Substitute.For<IGetTimeFactory>(), Substitute.For<ILogger>());

            var lastKnownEvent = Substitute.For<IJobSchedulerLastKnownEvent>();
            var queue = Substitute.For<IProducerMethodJobQueue>();
            queue.LastKnownEvent.Returns(lastKnownEvent);
            queue.Logger.Returns(Substitute.For<ILogger>());

            var schedule = Substitute.For<IJobSchedule>();
            schedule.OriginalText.Returns("* * * * *");
            // default to a far-future next event so tests that don't care never reschedule onto "now"
            schedule.Next().Returns(new DateTimeOffset(UtcNow.AddHours(1)));
            schedule.Next(Arg.Any<DateTimeOffset>()).Returns(new DateTimeOffset(UtcNow.AddHours(2)));

            var time = Substitute.For<IGetTime>();
            time.GetCurrentUtcDate().Returns(UtcNow);

            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> action =
                (message, notification) => NoOpAction();

            var job = new ScheduledJob(scheduler, "aJob", schedule, queue, action, time, "aRoute", false)
            {
                Window = window,
                IsAttached = attached
            };

            return new Harness
            {
                Queue = queue,
                Schedule = schedule,
                LastKnownEvent = lastKnownEvent,
                Job = job
            };
        }

        /// <remarks>
        /// Always assign this to a local before passing it to <c>Returns</c>. Building a
        /// substitute inside a <c>Returns(...)</c> argument overwrites NSubstitute's record of
        /// the call being configured and fails with "Could not find a call to return from".
        /// </remarks>
        private static IJobQueueOutputMessage Result(JobQueuedStatus status, Exception sendingException = null)
        {
            var result = Substitute.For<IJobQueueOutputMessage>();
            result.Status.Returns(status);
            result.SendingException.Returns(sendingException);
            return result;
        }

        /// <summary>Configures the queue to return <paramref name="result"/> from any send.</summary>
        private static void SendReturns(IProducerMethodJobQueue queue, IJobQueueOutputMessage result)
        {
            queue.SendAsync(Arg.Any<IScheduledJob>(), Arg.Any<DateTimeOffset>(),
                    Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                    Arg.Any<bool>())
                .Returns(Task.FromResult(result));
        }

        // --- StartSchedule ---

        [TestMethod]
        public void StartSchedule_NotAttached_Throws()
        {
            var harness = CreateJob(attached: false);

            Action act = () => harness.Job.StartSchedule();

            Assert.Throws<JobSchedulerException>(act);
            Assert.IsFalse(harness.Job.IsScheduleRunning);
        }

        [TestMethod]
        public void StartSchedule_WhenAttached_RunsAndSetsNextEvent()
        {
            var harness = CreateJob();

            harness.Job.StartSchedule();

            Assert.IsTrue(harness.Job.IsScheduleRunning);
            Assert.AreEqual(new DateTimeOffset(UtcNow.AddHours(1)), harness.Job.NextEvent);
        }

        [TestMethod]
        public void StartSchedule_CalledTwice_SecondCallIsANoOp()
        {
            var harness = CreateJob();
            harness.Job.StartSchedule();
            harness.Schedule.ClearReceivedCalls();

            harness.Job.StartSchedule();

            harness.Schedule.DidNotReceive().Next();
            Assert.IsTrue(harness.Job.IsScheduleRunning);
        }

        [TestMethod]
        public void StartSchedule_WindowSetAndPreviousEventInsideWindow_StartsFromPreviousEvent()
        {
            var harness = CreateJob(window: TimeSpan.FromMinutes(30));
            harness.LastKnownEvent.Get("aJob").Returns(new DateTimeOffset(UtcNow.AddHours(-2)));
            var missed = new DateTimeOffset(UtcNow.AddMinutes(-5));
            harness.Schedule.Previous().Returns(missed);

            harness.Job.StartSchedule();

            Assert.AreEqual(missed, harness.Job.NextEvent,
                "a missed event inside the window should be run immediately");
        }

        [TestMethod]
        public void StartSchedule_WindowSetButPreviousEventOlderThanWindow_UsesNextEvent()
        {
            var harness = CreateJob(window: TimeSpan.FromMinutes(30));
            harness.LastKnownEvent.Get("aJob").Returns(new DateTimeOffset(UtcNow.AddHours(-2)));
            // previous event is well outside the window, so it must not be replayed
            harness.Schedule.Previous().Returns(new DateTimeOffset(UtcNow.AddHours(-1)));

            harness.Job.StartSchedule();

            Assert.AreEqual(new DateTimeOffset(UtcNow.AddHours(1)), harness.Job.NextEvent);
        }

        [TestMethod]
        public void StartSchedule_WindowSetButNoLastKnownEvent_UsesNextEvent()
        {
            var harness = CreateJob(window: TimeSpan.FromMinutes(30));
            harness.LastKnownEvent.Get("aJob").Returns(default(DateTimeOffset));

            harness.Job.StartSchedule();

            Assert.AreEqual(new DateTimeOffset(UtcNow.AddHours(1)), harness.Job.NextEvent);
            harness.Schedule.DidNotReceive().Previous();
        }

        [TestMethod]
        public void StartSchedule_WindowSetButNoPreviousEvent_UsesNextEvent()
        {
            var harness = CreateJob(window: TimeSpan.FromMinutes(30));
            harness.LastKnownEvent.Get("aJob").Returns(new DateTimeOffset(UtcNow.AddHours(-2)));
            harness.Schedule.Previous().Returns((DateTimeOffset?)null);

            harness.Job.StartSchedule();

            Assert.AreEqual(new DateTimeOffset(UtcNow.AddHours(1)), harness.Job.NextEvent);
        }

        // --- StopSchedule ---

        [TestMethod]
        public void StopSchedule_WhenRunning_Stops()
        {
            var harness = CreateJob();
            harness.Job.StartSchedule();

            harness.Job.StopSchedule();

            Assert.IsFalse(harness.Job.IsScheduleRunning);
        }

        [TestMethod]
        public void StopSchedule_WhenNotRunning_IsANoOp()
        {
            var harness = CreateJob();

            harness.Job.StopSchedule();

            Assert.IsFalse(harness.Job.IsScheduleRunning);
        }

        // --- UpdateSchedule ---

        [TestMethod]
        public void UpdateSchedule_SameExpressionText_KeepsExistingSchedule()
        {
            var harness = CreateJob();

            harness.Job.UpdateSchedule("* * * * *");

            Assert.AreSame(harness.Schedule, harness.Job.Schedule);
        }

        [TestMethod]
        public void UpdateSchedule_NullSchedule_Throws()
        {
            var harness = CreateJob();

            Action act = () => harness.Job.UpdateSchedule((IJobSchedule)null);

            Assert.Throws<ArgumentNullException>(act);
        }

        [TestMethod]
        public void UpdateSchedule_WhenNotRunning_ReplacesScheduleAndStaysStopped()
        {
            var harness = CreateJob();
            var replacement = Substitute.For<IJobSchedule>();

            harness.Job.UpdateSchedule(replacement);

            Assert.AreSame(replacement, harness.Job.Schedule);
            Assert.IsFalse(harness.Job.IsScheduleRunning);
        }

        [TestMethod]
        public void UpdateSchedule_WhileRunning_ReplacesScheduleAndKeepsRunning()
        {
            var harness = CreateJob();
            harness.Job.StartSchedule();

            var replacement = Substitute.For<IJobSchedule>();
            var newNext = new DateTimeOffset(UtcNow.AddHours(5));
            replacement.Next().Returns(newNext);
            replacement.Next(Arg.Any<DateTimeOffset>()).Returns(newNext);

            harness.Job.UpdateSchedule(replacement);

            Assert.AreSame(replacement, harness.Job.Schedule);
            Assert.IsTrue(harness.Job.IsScheduleRunning, "an updated schedule restarts if it was running");
            Assert.AreEqual(newNext, harness.Job.NextEvent);
        }

        [TestMethod]
        public void UpdateSchedule_DifferentExpressionText_BuildsNewSchedule()
        {
            var harness = CreateJob();

            harness.Job.UpdateSchedule("*/5 * * * *");

            Assert.AreNotSame(harness.Schedule, harness.Job.Schedule);
            Assert.AreEqual("*/5 * * * *", harness.Job.Schedule.OriginalText);
        }

        // --- misc ---

        [TestMethod]
        public void ToString_ReturnsJobName()
        {
            Assert.AreEqual("aJob", CreateJob().Job.ToString());
        }

        [TestMethod]
        public void IsCallbackExecuting_WhenIdle_IsFalse()
        {
            Assert.IsFalse(CreateJob().Job.IsCallbackExecuting);
        }

        [TestMethod]
        public void Route_And_RawExpression_ComeFromConstructor()
        {
            var job = CreateJob().Job;

            Assert.AreEqual("aRoute", job.Route);
            Assert.IsFalse(job.RawExpression);
        }

        // --- RunPendingEventAsync ---

        [TestMethod]
        public async Task RunPendingEventAsync_StaleRunId_DoesNotSend()
        {
            var harness = CreateJob();
            harness.Job.StartSchedule();

            // run id only advances on stop, so this event belongs to a previous generation
            await harness.Job.RunPendingEventAsync(
                new PendingEvent(new DateTimeOffset(UtcNow), harness.Job, 42)).ConfigureAwait(false);

            await harness.Queue.DidNotReceive().SendAsync(Arg.Any<IScheduledJob>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                Arg.Any<bool>()).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task RunPendingEventAsync_Queued_RaisesEnQueueAndRecordsPreviousEvent()
        {
            var harness = CreateJob();
            harness.Job.StartSchedule();
            var eventTime = new DateTimeOffset(UtcNow);

            var result = Result(JobQueuedStatus.Success);
            SendReturns(harness.Queue, result);

            var raised = new TaskCompletionSource<IJobQueueOutputMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            harness.Job.OnEnQueue += (_, message) => raised.TrySetResult(message);

            await harness.Job.RunPendingEventAsync(new PendingEvent(eventTime, harness.Job, 0))
                .ConfigureAwait(false);

            var delivered = await raised.Task.WaitAsync(EventTimeout).ConfigureAwait(false);
            Assert.AreEqual(JobQueuedStatus.Success, delivered.Status);
            Assert.AreEqual(eventTime, harness.Job.PrevEvent);
        }

        [TestMethod]
        public async Task RunPendingEventAsync_RequeuedDueToError_RaisesEnQueue()
        {
            var harness = CreateJob();
            harness.Job.StartSchedule();

            var result = Result(JobQueuedStatus.RequeuedDueToErrorStatus);
            SendReturns(harness.Queue, result);

            var raised = new TaskCompletionSource<IJobQueueOutputMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            harness.Job.OnEnQueue += (_, message) => raised.TrySetResult(message);

            await harness.Job.RunPendingEventAsync(
                new PendingEvent(new DateTimeOffset(UtcNow), harness.Job, 0)).ConfigureAwait(false);

            var delivered = await raised.Task.WaitAsync(EventTimeout).ConfigureAwait(false);
            Assert.AreEqual(JobQueuedStatus.RequeuedDueToErrorStatus, delivered.Status);
        }

        [TestMethod]
        public async Task RunPendingEventAsync_AlreadyQueued_RaisesNonFatalFailure()
        {
            var harness = CreateJob();
            harness.Job.StartSchedule();

            var result = Result(JobQueuedStatus.AlreadyQueuedWaiting);
            SendReturns(harness.Queue, result);

            var raised = new TaskCompletionSource<IJobQueueOutputMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            harness.Job.OnNonFatalFailureEnQueue += (_, message) => raised.TrySetResult(message);

            await harness.Job.RunPendingEventAsync(
                new PendingEvent(new DateTimeOffset(UtcNow), harness.Job, 0)).ConfigureAwait(false);

            var delivered = await raised.Task.WaitAsync(EventTimeout).ConfigureAwait(false);
            Assert.AreEqual(JobQueuedStatus.AlreadyQueuedWaiting, delivered.Status);
        }

        [TestMethod]
        public async Task RunPendingEventAsync_SendReportsException_RaisesException()
        {
            var harness = CreateJob();
            harness.Job.StartSchedule();
            var sendFailure = new InvalidOperationException("could not send");

            var result = Result(JobQueuedStatus.Failed, sendFailure);
            SendReturns(harness.Queue, result);

            var raised = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
            harness.Job.OnException += (_, exception) => raised.TrySetResult(exception);

            await harness.Job.RunPendingEventAsync(
                new PendingEvent(new DateTimeOffset(UtcNow), harness.Job, 0)).ConfigureAwait(false);

            Assert.AreSame(sendFailure, await raised.Task.WaitAsync(EventTimeout).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task RunPendingEventAsync_SendThrows_RaisesException()
        {
            var harness = CreateJob();
            harness.Job.StartSchedule();
            var thrown = new InvalidOperationException("the transport exploded");

            harness.Queue.When(x => x.SendAsync(Arg.Any<IScheduledJob>(), Arg.Any<DateTimeOffset>(),
                    Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                    Arg.Any<bool>()))
                .Do(_ => throw thrown);

            var raised = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
            harness.Job.OnException += (_, exception) => raised.TrySetResult(exception);

            await harness.Job.RunPendingEventAsync(
                new PendingEvent(new DateTimeOffset(UtcNow), harness.Job, 0)).ConfigureAwait(false);

            Assert.AreSame(thrown, await raised.Task.WaitAsync(EventTimeout).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task RunPendingEventAsync_NextScheduledTimeUnavailable_TerminatesSchedule()
        {
            var harness = CreateJob();
            harness.Job.StartSchedule();

            var result = Result(JobQueuedStatus.Success);
            SendReturns(harness.Queue, result);

            var raised = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
            harness.Job.OnException += (_, exception) => raised.TrySetResult(exception);

            // the schedule can no longer produce a future occurrence
            harness.Schedule.When(x => x.Next())
                .Do(_ => throw new InvalidOperationException("no more occurrences"));

            await harness.Job.RunPendingEventAsync(
                new PendingEvent(new DateTimeOffset(UtcNow), harness.Job, 0)).ConfigureAwait(false);

            var exception = await raised.Task.WaitAsync(EventTimeout).ConfigureAwait(false);
            Assert.IsInstanceOfType<DotNetWorkQueueException>(exception);
            Assert.IsFalse(harness.Job.IsScheduleRunning, "an unusable schedule must terminate");
        }
    }
}
