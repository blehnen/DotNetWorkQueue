using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Policies;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NSubstitute;
using Polly.Registry;
using CreateJobMetaData = DotNetWorkQueue.Transport.Shared.Basic.CreateJobMetaData;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    [TestClass]
    public class PostgreSqlSendJobToQueueTests
    {
        /// <summary>
        /// Test subclass that exposes the protected DoesJobExist and DeleteJob methods so we can
        /// directly verify that PostgreSqlSendJobToQueue delegates to its injected handlers.
        /// </summary>
        private class TestablePostgreSqlSendJobToQueue : PostgreSqlSendJobToQueue
        {
            public TestablePostgreSqlSendJobToQueue(
                IProducerMethodQueue queue,
                IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>, QueueStatuses> doesJobExist,
                IQueryHandler<GetJobIdQuery<long>, long> getJobId,
                CreateJobMetaData createJobMetaData,
                IGetTimeFactory getTimeFactory,
                IRemoveMessage removeMessage)
                : base(queue, doesJobExist, getJobId, createJobMetaData, getTimeFactory, removeMessage)
            {
            }

            public QueueStatuses InvokeDoesJobExist(string name, DateTimeOffset scheduledTime)
            {
                return DoesJobExist(name, scheduledTime);
            }

            public void InvokeDeleteJob(string name)
            {
                DeleteJob(name);
            }

            public bool InvokeJobAlreadyExistsError(Exception error)
            {
                return JobAlreadyExistsError(error);
            }
        }

        private class Fixture
        {
            public IProducerMethodQueue Queue { get; set; }
            public IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>, QueueStatuses> DoesJobExist { get; set; }
            public IQueryHandler<GetJobIdQuery<long>, long> GetJobId { get; set; }
            public CreateJobMetaData CreateJobMetaData { get; set; }
            public IGetTimeFactory GetTimeFactory { get; set; }
            public IRemoveMessage RemoveMessage { get; set; }
        }

        private static Fixture CreateFixture()
        {
            var queue = Substitute.For<IProducerMethodQueue>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var transportConfig = new TransportConfigurationSend(connInfo);
            var headers = Substitute.For<IHeaders>();
            var additionalConfig = Substitute.For<IConfiguration>();
            var timeConfig = new BaseTimeConfiguration();
            var policies = new Policies.Policies(
                new ResiliencePipelineRegistry<string>(), new PolicyDefinitions());
            var config = new QueueProducerConfiguration(transportConfig, headers,
                additionalConfig, timeConfig, policies);
            queue.Configuration.Returns(config);

            var getTimeFactory = Substitute.For<IGetTimeFactory>();
            var time = Substitute.For<IGetTime>();
            time.GetCurrentUtcDate().Returns(DateTime.UtcNow);
            getTimeFactory.Create().Returns(time);

            var jobMeta = Substitute.For<IJobSchedulerMetaData>();
            var createJobMetaData = new CreateJobMetaData(jobMeta);

            return new Fixture
            {
                Queue = queue,
                DoesJobExist = Substitute.For<IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>, QueueStatuses>>(),
                GetJobId = Substitute.For<IQueryHandler<GetJobIdQuery<long>, long>>(),
                CreateJobMetaData = createJobMetaData,
                GetTimeFactory = getTimeFactory,
                RemoveMessage = Substitute.For<IRemoveMessage>(),
            };
        }

        private static TestablePostgreSqlSendJobToQueue CreateHandler(Fixture f)
        {
            return new TestablePostgreSqlSendJobToQueue(
                f.Queue,
                f.DoesJobExist,
                f.GetJobId,
                f.CreateJobMetaData,
                f.GetTimeFactory,
                f.RemoveMessage);
        }

        [TestMethod]
        public void Constructor_Creates_Instance()
        {
            var f = CreateFixture();
            var handler = CreateHandler(f);
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void DoesJobExist_DelegatesToQueryHandler()
        {
            var f = CreateFixture();
            f.DoesJobExist
                .Handle(Arg.Any<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>>())
                .Returns(QueueStatuses.Processing);

            var handler = CreateHandler(f);

            var result = handler.InvokeDoesJobExist("MyJob", DateTimeOffset.UtcNow);

            Assert.AreEqual(QueueStatuses.Processing, result);
            f.DoesJobExist.Received(1)
                .Handle(Arg.Any<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>>());
        }

        [TestMethod]
        public void DoesJobExist_PassesCorrectQuery()
        {
            var f = CreateFixture();
            var jobName = "TestJob_" + Guid.NewGuid().ToString("N");
            var scheduled = DateTimeOffset.UtcNow.AddMinutes(7);

            DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction> captured = null;
            f.DoesJobExist
                .Handle(Arg.Do<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>>(q => captured = q))
                .Returns(QueueStatuses.NotQueued);

            var handler = CreateHandler(f);
            handler.InvokeDoesJobExist(jobName, scheduled);

            Assert.IsNotNull(captured);
            Assert.AreEqual(jobName, captured.JobName);
            Assert.AreEqual(scheduled, captured.ScheduledTime);
        }

        [TestMethod]
        public void DeleteJob_RetrievesJobIdAndRemovesMessage()
        {
            var f = CreateFixture();
            const long expectedJobId = 4242L;
            var jobName = "JobToDelete";

            GetJobIdQuery<long> capturedQuery = null;
            f.GetJobId
                .Handle(Arg.Do<GetJobIdQuery<long>>(q => capturedQuery = q))
                .Returns(expectedJobId);

            IMessageId capturedId = null;
            RemoveMessageReason capturedReason = RemoveMessageReason.Complete;
            f.RemoveMessage
                .Remove(
                    Arg.Do<IMessageId>(id => capturedId = id),
                    Arg.Do<RemoveMessageReason>(r => capturedReason = r))
                .Returns(RemoveMessageStatus.Removed);

            var handler = CreateHandler(f);
            handler.InvokeDeleteJob(jobName);

            // Verify GetJobId was called with the correct job name
            Assert.IsNotNull(capturedQuery);
            Assert.AreEqual(jobName, capturedQuery.JobName);

            // Verify RemoveMessage was called with the id from GetJobId and Error reason
            f.RemoveMessage.Received(1).Remove(Arg.Any<IMessageId>(), RemoveMessageReason.Error);
            Assert.IsNotNull(capturedId);
            Assert.AreEqual(expectedJobId, capturedId.Id.Value);
            Assert.AreEqual(RemoveMessageReason.Error, capturedReason);
        }

        [TestMethod]
        public void JobAlreadyExistsError_True_For_Duplicate_Key_Jobname()
        {
            var f = CreateFixture();
            var handler = CreateHandler(f);

            var ex = new Exception("ERROR: duplicate key value violates unique constraint \"jobname_idx\"");
            Assert.IsTrue(handler.InvokeJobAlreadyExistsError(ex));
        }

        [TestMethod]
        public void JobAlreadyExistsError_True_For_Failed_To_Insert_Message()
        {
            var f = CreateFixture();
            var handler = CreateHandler(f);

            var ex = new Exception("Failed to insert record - the job has already been queued or processed");
            Assert.IsTrue(handler.InvokeJobAlreadyExistsError(ex));
        }

        [TestMethod]
        public void JobAlreadyExistsError_False_For_Other_Error()
        {
            var f = CreateFixture();
            var handler = CreateHandler(f);

            var ex = new Exception("connection refused");
            Assert.IsFalse(handler.InvokeJobAlreadyExistsError(ex));
        }
    }
}
