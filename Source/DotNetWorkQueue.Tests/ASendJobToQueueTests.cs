using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Policies;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly.Registry;

namespace DotNetWorkQueue.Tests
{
    [TestClass]
    public class ASendJobToQueueTests
    {
        /// <summary>
        /// Concrete test subclass that exposes the abstract methods for testing.
        /// </summary>
        private class TestableSendJobToQueue : ASendJobToQueue
        {
            public QueueStatuses DoesJobExistResult { get; set; } = QueueStatuses.NotQueued;
            public bool JobAlreadyExistsErrorResult { get; set; } = false;
            public int DeleteJobCallCount { get; private set; }
            public int DoesJobExistCallCount { get; private set; }
            public string LastDeletedJobName { get; private set; }
            public string LastSetMetaDataJobName { get; private set; }

            public TestableSendJobToQueue(IProducerMethodQueue queue, IGetTimeFactory getTimeFactory)
                : base(queue, getTimeFactory)
            {
            }

            protected override QueueStatuses DoesJobExist(string name, DateTimeOffset scheduledTime)
            {
                DoesJobExistCallCount++;
                return DoesJobExistResult;
            }

            protected override void DeleteJob(string name)
            {
                DeleteJobCallCount++;
                LastDeletedJobName = name;
            }

            protected override bool JobAlreadyExistsError(Exception error)
            {
                return JobAlreadyExistsErrorResult;
            }

            protected override void SetMetaDataForJob(string jobName, DateTimeOffset scheduledTime,
                DateTimeOffset eventTime, string route, IAdditionalMessageData messageData)
            {
                LastSetMetaDataJobName = jobName;
            }
        }

        private static (TestableSendJobToQueue handler, IProducerMethodQueue queue, IScheduledJob job)
            CreateHandler()
        {
            var queue = Substitute.For<IProducerMethodQueue>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var transportConfig = new TransportConfigurationSend(connInfo);
            var headers = Substitute.For<IHeaders>();
            var additionalConfig = Substitute.For<IConfiguration>();
            var timeConfig = new BaseTimeConfiguration();
            var policies = new DotNetWorkQueue.Policies.Policies(
                new ResiliencePipelineRegistry<string>(), new PolicyDefinitions());
            var config = new QueueProducerConfiguration(transportConfig, headers,
                additionalConfig, timeConfig, policies);
            queue.Configuration.Returns(config);

            var getTime = Substitute.For<IGetTimeFactory>();
            var time = Substitute.For<IGetTime>();
            time.GetCurrentUtcDate().Returns(DateTime.UtcNow);
            getTime.Create().Returns(time);

            var handler = new TestableSendJobToQueue(queue, getTime);

            var job = Substitute.For<IScheduledJob>();
            job.Name.Returns("TestJob");
            job.Route.Returns((string)null);

            return (handler, queue, job);
        }

        private static IQueueOutputMessage CreateSuccessMessage()
        {
            var msg = Substitute.For<IQueueOutputMessage>();
            msg.HasError.Returns(false);
            msg.SentMessage.Returns(Substitute.For<ISentMessage>());
            return msg;
        }

        private static IQueueOutputMessage CreateErrorMessage(Exception ex = null)
        {
            var msg = Substitute.For<IQueueOutputMessage>();
            msg.HasError.Returns(true);
            msg.SendingException.Returns(ex ?? new Exception("test"));
            msg.SentMessage.Returns(Substitute.For<ISentMessage>());
            return msg;
        }

        [TestMethod]
        public void IsDisposed_Returns_Queue_IsDisposed()
        {
            var (handler, queue, _) = CreateHandler();
            queue.IsDisposed.Returns(false);
            Assert.IsFalse(handler.IsDisposed);
            queue.IsDisposed.Returns(true);
            Assert.IsTrue(handler.IsDisposed);
        }

        [TestMethod]
        public void Configuration_Returns_Queue_Configuration()
        {
            var (handler, queue, _) = CreateHandler();
            Assert.IsNotNull(handler.Configuration);
        }

        [TestMethod]
        public void Dispose_Disposes_Queue()
        {
            var (handler, queue, _) = CreateHandler();
            handler.Dispose();
            queue.Received(1).Dispose();
        }

        [TestMethod]
        public void Dispose_Is_Idempotent()
        {
            var (handler, queue, _) = CreateHandler();
            handler.Dispose();
            handler.Dispose();
            queue.Received(1).Dispose();
        }

        [TestMethod]
        public async Task SendAsync_Job_Processing_Returns_AlreadyQueuedProcessing()
        {
            var (handler, _, job) = CreateHandler();
            handler.DoesJobExistResult = QueueStatuses.Processing;

            var result = await handler.SendAsync(job, DateTimeOffset.UtcNow,
                (Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>)null);

            Assert.AreEqual(JobQueuedStatus.AlreadyQueuedProcessing, result.Status);
        }

        [TestMethod]
        public async Task SendAsync_Job_Waiting_Returns_AlreadyQueuedWaiting()
        {
            var (handler, _, job) = CreateHandler();
            handler.DoesJobExistResult = QueueStatuses.Waiting;

            var result = await handler.SendAsync(job, DateTimeOffset.UtcNow,
                (Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>)null);

            Assert.AreEqual(JobQueuedStatus.AlreadyQueuedWaiting, result.Status);
        }

        [TestMethod]
        public async Task SendAsync_Job_Processed_Returns_AlreadyProcessed()
        {
            var (handler, _, job) = CreateHandler();
            handler.DoesJobExistResult = QueueStatuses.Processed;

            var result = await handler.SendAsync(job, DateTimeOffset.UtcNow,
                (Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>)null);

            Assert.AreEqual(JobQueuedStatus.AlreadyProcessed, result.Status);
        }

        [TestMethod]
        public async Task SendAsync_Job_Error_Deletes_Job_Then_Sends()
        {
            var (handler, queue, job) = CreateHandler();
            handler.DoesJobExistResult = QueueStatuses.Error;

            var successMsg = CreateSuccessMessage();
            queue.SendAsync(
                Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                Arg.Any<IAdditionalMessageData>(),
                Arg.Any<bool>())
                .Returns(Task.FromResult(successMsg));

            var result = await handler.SendAsync(job, DateTimeOffset.UtcNow,
                (Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>)
                    ((m, w) => Console.WriteLine()));

            Assert.AreEqual(1, handler.DeleteJobCallCount);
            Assert.AreEqual(JobQueuedStatus.Success, result.Status);
        }

        [TestMethod]
        public async Task SendAsync_NotQueued_Success_Returns_Success()
        {
            var (handler, queue, job) = CreateHandler();
            handler.DoesJobExistResult = QueueStatuses.NotQueued;

            var successMsg = CreateSuccessMessage();
            queue.SendAsync(
                Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                Arg.Any<IAdditionalMessageData>(),
                Arg.Any<bool>())
                .Returns(Task.FromResult(successMsg));

            var result = await handler.SendAsync(job, DateTimeOffset.UtcNow,
                (Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>)
                    ((m, w) => Console.WriteLine()));

            Assert.AreEqual(JobQueuedStatus.Success, result.Status);
        }

        [TestMethod]
        public async Task SendAsync_Error_NotAlreadyExists_Returns_Failed()
        {
            var (handler, queue, job) = CreateHandler();
            handler.DoesJobExistResult = QueueStatuses.NotQueued;
            handler.JobAlreadyExistsErrorResult = false;

            var errorMsg = CreateErrorMessage();
            queue.SendAsync(
                Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                Arg.Any<IAdditionalMessageData>(),
                Arg.Any<bool>())
                .Returns(Task.FromResult(errorMsg));

            var result = await handler.SendAsync(job, DateTimeOffset.UtcNow,
                (Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>)
                    ((m, w) => Console.WriteLine()));

            Assert.AreEqual(JobQueuedStatus.Failed, result.Status);
        }

        [TestMethod]
        public async Task SendAsync_Error_AlreadyExists_Processing_Returns_AlreadyQueuedProcessing()
        {
            var (handler, queue, job) = CreateHandler();
            handler.JobAlreadyExistsErrorResult = true;

            // First call to DoesJobExist returns NotQueued (pass pre-checks), subsequent calls return Processing
            handler.DoesJobExistResult = QueueStatuses.NotQueued;

            var errorMsg = CreateErrorMessage();
            queue.SendAsync(
                Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                Arg.Any<IAdditionalMessageData>(),
                Arg.Any<bool>())
                .Returns(_ =>
                {
                    // After first send, change the status so ProcessResult sees Processing
                    handler.DoesJobExistResult = QueueStatuses.Processing;
                    return Task.FromResult(errorMsg);
                });

            var result = await handler.SendAsync(job, DateTimeOffset.UtcNow,
                (Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>)
                    ((m, w) => Console.WriteLine()));

            Assert.AreEqual(JobQueuedStatus.AlreadyQueuedProcessing, result.Status);
        }

        [TestMethod]
        public async Task SendAsync_Error_AlreadyExists_Waiting_Returns_AlreadyQueuedWaiting()
        {
            var (handler, queue, job) = CreateHandler();
            handler.JobAlreadyExistsErrorResult = true;
            handler.DoesJobExistResult = QueueStatuses.NotQueued;

            var errorMsg = CreateErrorMessage();
            queue.SendAsync(
                Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                Arg.Any<IAdditionalMessageData>(),
                Arg.Any<bool>())
                .Returns(_ =>
                {
                    handler.DoesJobExistResult = QueueStatuses.Waiting;
                    return Task.FromResult(errorMsg);
                });

            var result = await handler.SendAsync(job, DateTimeOffset.UtcNow,
                (Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>)
                    ((m, w) => Console.WriteLine()));

            Assert.AreEqual(JobQueuedStatus.AlreadyQueuedWaiting, result.Status);
        }

        [TestMethod]
        public async Task SendAsync_Error_AlreadyExists_Processed_Returns_AlreadyProcessed()
        {
            var (handler, queue, job) = CreateHandler();
            handler.JobAlreadyExistsErrorResult = true;
            handler.DoesJobExistResult = QueueStatuses.NotQueued;

            var errorMsg = CreateErrorMessage();
            queue.SendAsync(
                Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                Arg.Any<IAdditionalMessageData>(),
                Arg.Any<bool>())
                .Returns(_ =>
                {
                    handler.DoesJobExistResult = QueueStatuses.Processed;
                    return Task.FromResult(errorMsg);
                });

            var result = await handler.SendAsync(job, DateTimeOffset.UtcNow,
                (Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>)
                    ((m, w) => Console.WriteLine()));

            Assert.AreEqual(JobQueuedStatus.AlreadyProcessed, result.Status);
        }

        [TestMethod]
        public async Task SendAsync_Error_AlreadyExists_Error_Deletes_And_Retries()
        {
            var (handler, queue, job) = CreateHandler();
            handler.JobAlreadyExistsErrorResult = true;
            handler.DoesJobExistResult = QueueStatuses.NotQueued;

            var errorMsg = CreateErrorMessage();
            var successMsg = CreateSuccessMessage();
            var sendCount = 0;
            queue.SendAsync(
                Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                Arg.Any<IAdditionalMessageData>(),
                Arg.Any<bool>())
                .Returns(_ =>
                {
                    sendCount++;
                    if (sendCount == 1)
                    {
                        handler.DoesJobExistResult = QueueStatuses.Error;
                        return Task.FromResult(errorMsg);
                    }
                    // Second send succeeds
                    handler.DoesJobExistResult = QueueStatuses.NotQueued;
                    return Task.FromResult(successMsg);
                });

            var result = await handler.SendAsync(job, DateTimeOffset.UtcNow,
                (Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>)
                    ((m, w) => Console.WriteLine()));

            // Should have deleted the errored job and retried
            Assert.IsGreaterThanOrEqualTo(1, handler.DeleteJobCallCount);
            Assert.AreEqual(JobQueuedStatus.Success, result.Status);
        }

        [TestMethod]
        public async Task SendAsync_Error_AlreadyExists_NotQueued_Retries_Then_Fails()
        {
            var (handler, queue, job) = CreateHandler();
            handler.JobAlreadyExistsErrorResult = true;
            handler.DoesJobExistResult = QueueStatuses.NotQueued;

            var errorMsg = CreateErrorMessage();
            queue.SendAsync(
                Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                Arg.Any<IAdditionalMessageData>(),
                Arg.Any<bool>())
                .Returns(Task.FromResult(errorMsg));

            var result = await handler.SendAsync(job, DateTimeOffset.UtcNow,
                (Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>)
                    ((m, w) => Console.WriteLine()));

            // Both sends fail with AlreadyExists + NotQueued status => returns null from ProcessResult => Failed
            Assert.AreEqual(JobQueuedStatus.Failed, result.Status);
        }

        [TestMethod]
        public async Task SendAsync_Sets_MetaData()
        {
            var (handler, queue, job) = CreateHandler();
            handler.DoesJobExistResult = QueueStatuses.NotQueued;

            var successMsg = CreateSuccessMessage();
            queue.SendAsync(
                Arg.Any<Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>>(),
                Arg.Any<IAdditionalMessageData>(),
                Arg.Any<bool>())
                .Returns(Task.FromResult(successMsg));

            await handler.SendAsync(job, DateTimeOffset.UtcNow,
                (Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>>)
                    ((m, w) => Console.WriteLine()));

            Assert.AreEqual("TestJob", handler.LastSetMetaDataJobName);
        }
    }
}
