using System;
using System.Linq;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.Basic.Query;
using DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared;
using LiteDB;
using NSubstitute;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic.QueryHandler
{
    [TestClass]
    public class DoesJobExistQueryHandlerTests
    {
        private const string QueueName = "testQueue";

        [TestMethod]
        public void Create_Default()
        {
            var tableExists = Substitute.For<IQueryHandler<GetTableExistsQuery, bool>>();
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.QueueName.Returns(QueueName);
            var tableNameHelper = new TableNameHelper(connInfo);

            var handler = new DoesJobExistQueryHandler(tableExists, tableNameHelper);
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void Create_Null_TableExists_Throws()
        {
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.QueueName.Returns(QueueName);
            var tableNameHelper = new TableNameHelper(connInfo);

            Assert.ThrowsExactly<ArgumentNullException>(
                () => new DoesJobExistQueryHandler(null, tableNameHelper));
        }

        [TestMethod]
        public void Create_Null_TableNameHelper_Throws()
        {
            var tableExists = Substitute.For<IQueryHandler<GetTableExistsQuery, bool>>();

            Assert.ThrowsExactly<ArgumentNullException>(
                () => new DoesJobExistQueryHandler(tableExists, null));
        }

        [TestMethod]
        public void Handle_NoStatus_NoJob_Returns_NotQueued()
        {
            var (handler, tableExists) = CreateHandlerWithDb(out var db);
            using (db)
            {
                tableExists.Handle(Arg.Any<GetTableExistsQuery>()).Returns(false);

                var query = new DoesJobExistQuery("myJob", DateTimeOffset.UtcNow, db);
                var result = handler.Handle(query);

                result.Should().Be(QueueStatuses.NotQueued);
            }
        }

        [TestMethod]
        public void Handle_StatusExists_Returns_Status()
        {
            var (handler, tableExists) = CreateHandlerWithDb(out var db);
            using (db)
            {
                var connInfo = Substitute.For<IConnectionInformation>();
                connInfo.QueueName.Returns(QueueName);
                var tnh = new TableNameHelper(connInfo);

                // Insert a status record
                var col = db.GetCollection<StatusTable>(tnh.StatusName);
                col.Insert(new StatusTable
                {
                    QueueId = 1,
                    JobName = "myJob",
                    Status = QueueStatuses.Waiting
                });

                var query = new DoesJobExistQuery("myJob", DateTimeOffset.UtcNow, db);
                var result = handler.Handle(query);

                result.Should().Be(QueueStatuses.Waiting);
            }
        }

        [TestMethod]
        public void Handle_StatusProcessing_Returns_Processing()
        {
            var (handler, tableExists) = CreateHandlerWithDb(out var db);
            using (db)
            {
                var connInfo = Substitute.For<IConnectionInformation>();
                connInfo.QueueName.Returns(QueueName);
                var tnh = new TableNameHelper(connInfo);

                var col = db.GetCollection<StatusTable>(tnh.StatusName);
                col.Insert(new StatusTable
                {
                    QueueId = 1,
                    JobName = "myJob",
                    Status = QueueStatuses.Processing
                });

                var query = new DoesJobExistQuery("myJob", DateTimeOffset.UtcNow, db);
                var result = handler.Handle(query);

                result.Should().Be(QueueStatuses.Processing);
            }
        }

        [TestMethod]
        public void Handle_NotQueued_JobTableExists_MatchingSchedule_Returns_Processed()
        {
            var (handler, tableExists) = CreateHandlerWithDb(out var db);
            using (db)
            {
                tableExists.Handle(Arg.Any<GetTableExistsQuery>()).Returns(true);

                var connInfo = Substitute.For<IConnectionInformation>();
                connInfo.QueueName.Returns(QueueName);
                var tnh = new TableNameHelper(connInfo);

                var scheduledTime = new DateTimeOffset(2026, 3, 25, 10, 0, 0, TimeSpan.Zero);

                // Insert a job record with matching schedule time
                var colJob = db.GetCollection<JobsTable>(tnh.JobTableName);
                colJob.Insert(new JobsTable
                {
                    JobName = "myJob",
                    JobScheduledTime = scheduledTime,
                    JobEventTime = scheduledTime
                });

                var query = new DoesJobExistQuery("myJob", scheduledTime, db);
                var result = handler.Handle(query);

                result.Should().Be(QueueStatuses.Processed);
            }
        }

        [TestMethod]
        public void Handle_NotQueued_JobTableExists_DifferentSchedule_Returns_NotQueued()
        {
            var (handler, tableExists) = CreateHandlerWithDb(out var db);
            using (db)
            {
                tableExists.Handle(Arg.Any<GetTableExistsQuery>()).Returns(true);

                var connInfo = Substitute.For<IConnectionInformation>();
                connInfo.QueueName.Returns(QueueName);
                var tnh = new TableNameHelper(connInfo);

                var scheduledTime = new DateTimeOffset(2026, 3, 25, 10, 0, 0, TimeSpan.Zero);
                var differentTime = new DateTimeOffset(2026, 3, 25, 11, 0, 0, TimeSpan.Zero);

                var colJob = db.GetCollection<JobsTable>(tnh.JobTableName);
                colJob.Insert(new JobsTable
                {
                    JobName = "myJob",
                    JobScheduledTime = scheduledTime,
                    JobEventTime = scheduledTime
                });

                var query = new DoesJobExistQuery("myJob", differentTime, db);
                var result = handler.Handle(query);

                result.Should().Be(QueueStatuses.NotQueued);
            }
        }

        [TestMethod]
        public void Handle_NotQueued_JobTableDoesNotExist_Returns_NotQueued()
        {
            var (handler, tableExists) = CreateHandlerWithDb(out var db);
            using (db)
            {
                tableExists.Handle(Arg.Any<GetTableExistsQuery>()).Returns(false);

                var query = new DoesJobExistQuery("myJob", DateTimeOffset.UtcNow, db);
                var result = handler.Handle(query);

                result.Should().Be(QueueStatuses.NotQueued);
            }
        }

        [TestMethod]
        public void Handle_NotQueued_JobTableExists_NoJobRecord_Returns_NotQueued()
        {
            var (handler, tableExists) = CreateHandlerWithDb(out var db);
            using (db)
            {
                tableExists.Handle(Arg.Any<GetTableExistsQuery>()).Returns(true);

                // Job table exists but no matching job record
                var query = new DoesJobExistQuery("nonexistentJob", DateTimeOffset.UtcNow, db);
                var result = handler.Handle(query);

                result.Should().Be(QueueStatuses.NotQueued);
            }
        }

        private static (DoesJobExistQueryHandler handler, IQueryHandler<GetTableExistsQuery, bool> tableExists)
            CreateHandlerWithDb(out LiteDatabase db)
        {
            db = new LiteDatabase("Filename=:memory:");

            var tableExists = Substitute.For<IQueryHandler<GetTableExistsQuery, bool>>();
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.QueueName.Returns(QueueName);
            var tableNameHelper = new TableNameHelper(connInfo);

            var handler = new DoesJobExistQueryHandler(tableExists, tableNameHelper);
            return (handler, tableExists);
        }
    }
}
