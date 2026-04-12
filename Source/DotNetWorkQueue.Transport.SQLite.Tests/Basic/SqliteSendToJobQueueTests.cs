using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class SqliteSendToJobQueueTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var sut = CreateSut(out _);
            Assert.IsNotNull(sut);
        }

        [TestMethod]
        public void DoesJobExist_DelegatesToQueryHandler()
        {
            var sut = CreateSut(out var deps);
            deps.DoesJobExist
                .Handle(Arg.Any<DoesJobExistQuery<IDbConnection, IDbTransaction>>())
                .Returns(QueueStatuses.Processed);

            var result = sut.DoesJobExistTest("job-name", DateTimeOffset.UtcNow);

            Assert.AreEqual(QueueStatuses.Processed, result);
            deps.DoesJobExist.Received(1)
                .Handle(Arg.Any<DoesJobExistQuery<IDbConnection, IDbTransaction>>());
        }

        [TestMethod]
        public void DoesJobExist_PassesCorrectQuery()
        {
            var sut = CreateSut(out var deps);
            const string jobName = "my-recurring-job";
            var scheduled = new DateTimeOffset(2025, 1, 15, 12, 30, 0, TimeSpan.Zero);

            DoesJobExistQuery<IDbConnection, IDbTransaction> captured = null;
            deps.DoesJobExist
                .Handle(Arg.Do<DoesJobExistQuery<IDbConnection, IDbTransaction>>(q => captured = q))
                .Returns(QueueStatuses.Waiting);

            sut.DoesJobExistTest(jobName, scheduled);

            Assert.IsNotNull(captured);
            Assert.AreEqual(jobName, captured.JobName);
            Assert.AreEqual(scheduled, captured.ScheduledTime);
        }

        [TestMethod]
        public void DeleteJob_RetrievesJobIdAndRemovesMessage()
        {
            var sut = CreateSut(out var deps);
            const string jobName = "doomed-job";
            const long jobId = 42L;

            GetJobIdQuery<long> capturedQuery = null;
            deps.GetJobId
                .Handle(Arg.Do<GetJobIdQuery<long>>(q => capturedQuery = q))
                .Returns(jobId);

            IMessageId capturedMessageId = null;
            RemoveMessageReason capturedReason = RemoveMessageReason.Complete;
            deps.RemoveMessage
                .When(r => r.Remove(Arg.Any<IMessageId>(), Arg.Any<RemoveMessageReason>()))
                .Do(ci =>
                {
                    capturedMessageId = ci.Arg<IMessageId>();
                    capturedReason = ci.Arg<RemoveMessageReason>();
                });

            sut.DeleteJobTest(jobName);

            Assert.IsNotNull(capturedQuery);
            Assert.AreEqual(jobName, capturedQuery.JobName);
            deps.GetJobId.Received(1).Handle(Arg.Any<GetJobIdQuery<long>>());

            Assert.IsNotNull(capturedMessageId);
            Assert.AreEqual(RemoveMessageReason.Error, capturedReason);
            var typed = capturedMessageId as MessageQueueId<long>;
            Assert.IsNotNull(typed);
            Assert.AreEqual(jobId.ToString(), typed.ToString());
            Assert.IsTrue(typed.HasValue);
        }

        private static TestableSqliteSendToJobQueue CreateSut(out Deps deps)
        {
            deps = new Deps
            {
                Queue = Substitute.For<IProducerMethodQueue>(),
                DoesJobExist =
                    Substitute.For<IQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>, QueueStatuses>>(),
                RemoveMessage = Substitute.For<IRemoveMessage>(),
                GetJobId = Substitute.For<IQueryHandler<GetJobIdQuery<long>, long>>(),
                CreateJobMetaData = new CreateJobMetaData(Substitute.For<IJobSchedulerMetaData>()),
                GetTimeFactory = Substitute.For<IGetTimeFactory>()
            };

            return new TestableSqliteSendToJobQueue(
                deps.Queue,
                deps.DoesJobExist,
                deps.RemoveMessage,
                deps.GetJobId,
                deps.CreateJobMetaData,
                deps.GetTimeFactory);
        }

        private sealed class Deps
        {
            public IProducerMethodQueue Queue { get; set; }
            public IQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>, QueueStatuses> DoesJobExist { get; set; }
            public IRemoveMessage RemoveMessage { get; set; }
            public IQueryHandler<GetJobIdQuery<long>, long> GetJobId { get; set; }
            public CreateJobMetaData CreateJobMetaData { get; set; }
            public IGetTimeFactory GetTimeFactory { get; set; }
        }

        // Test subclass to expose protected members
        private sealed class TestableSqliteSendToJobQueue : SqliteSendToJobQueue
        {
            public TestableSqliteSendToJobQueue(
                IProducerMethodQueue queue,
                IQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>, QueueStatuses> doesJobExist,
                IRemoveMessage removeMessage,
                IQueryHandler<GetJobIdQuery<long>, long> getJobId,
                CreateJobMetaData createJobMetaData,
                IGetTimeFactory getTimeFactory)
                : base(queue, doesJobExist, removeMessage, getJobId, createJobMetaData, getTimeFactory)
            {
            }

            public QueueStatuses DoesJobExistTest(string name, DateTimeOffset scheduledTime)
                => DoesJobExist(name, scheduledTime);

            public void DeleteJobTest(string name) => DeleteJob(name);
        }
    }
}
