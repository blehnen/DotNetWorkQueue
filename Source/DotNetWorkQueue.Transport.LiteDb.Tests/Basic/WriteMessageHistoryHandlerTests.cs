using System;
using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using NSubstitute;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic
{
    [TestClass]
    public class WriteMessageHistoryHandlerTests
    {
        private const string QueueName = "testQueue";

        private static (WriteMessageHistoryHandler handler, LiteDbConnectionManager connectionManager,
            TableNameHelper tableNameHelper) CreateHandler(
            bool enableHistory = true, bool storeBody = false)
        {
            // Use a unique filename per test to avoid cross-test collisions
            var connString = $"Filename=:memory:;Connection=direct";

            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.ConnectionString.Returns(connString);
            connInfo.QueueName.Returns(QueueName);

            var scope = Substitute.For<ICreationScope>();
            var connectionManager = new LiteDbConnectionManager(connInfo, scope);
            var tableNameHelper = new TableNameHelper(connInfo);

            var historyOptions = Substitute.For<IHistoryTransportOptions>();
            historyOptions.StoreBody.Returns(storeBody);

            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(enableHistory);
            options.HistoryOptions.Returns(historyOptions);

            var handler = new WriteMessageHistoryHandler(connectionManager, tableNameHelper, options);
            return (handler, connectionManager, tableNameHelper);
        }

        // === RecordEnqueue ===

        [TestMethod]
        public void RecordEnqueue_Inserts_Record()
        {
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", "routeA", "MyType", new byte[] { 1, 2 }, new byte[] { 3, 4 });

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var records = col.FindAll().ToList();
                    records.Should().HaveCount(1);
                    records[0].QueueId.Should().Be("q1");
                    records[0].CorrelationId.Should().Be("c1");
                    records[0].Route.Should().Be("routeA");
                    records[0].MessageType.Should().Be("MyType");
                    records[0].Status.Should().Be((int)MessageHistoryStatus.Enqueued);
                    records[0].RetryCount.Should().Be(0);
                    records[0].EnqueuedUtc.Should().BeGreaterThan(0);
                }
            }
        }

        [TestMethod]
        public void RecordEnqueue_StoreBody_True_Stores_Body_And_Headers()
        {
            var (handler, cm, tnh) = CreateHandler(storeBody: true);
            using (cm)
            {
                var body = new byte[] { 10, 20, 30 };
                var headers = new byte[] { 40, 50 };
                handler.RecordEnqueue("q1", "c1", null, "T", body, headers);

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    record.Body.Should().BeEquivalentTo(body);
                    record.Headers.Should().BeEquivalentTo(headers);
                }
            }
        }

        [TestMethod]
        public void RecordEnqueue_StoreBody_False_NullsBodyAndHeaders()
        {
            var (handler, cm, tnh) = CreateHandler(storeBody: false);
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", null, "T", new byte[] { 1 }, new byte[] { 2 });

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    record.Body.Should().BeNull();
                    record.Headers.Should().BeNull();
                }
            }
        }

        // === RecordProcessingStart ===

        [TestMethod]
        public void RecordProcessingStart_Updates_Status_And_StartedUtc()
        {
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", null, "T", null, null);
                handler.RecordProcessingStart("q1");

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    record.Status.Should().Be((int)MessageHistoryStatus.Processing);
                    record.StartedUtc.Should().BeGreaterThan(0);
                }
            }
        }

        [TestMethod]
        public void RecordProcessingStart_NoRecord_DoesNotThrow()
        {
            var (handler, cm, _) = CreateHandler();
            using (cm)
            {
                Action act = () => handler.RecordProcessingStart("nonexistent");
                act.Should().NotThrow();
            }
        }

        // === RecordComplete ===

        [TestMethod]
        public void RecordComplete_Sets_Complete_Status_And_Duration()
        {
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", null, "T", null, null);
                handler.RecordProcessingStart("q1");
                handler.RecordComplete("q1");

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    record.Status.Should().Be((int)MessageHistoryStatus.Complete);
                    record.CompletedUtc.Should().BeGreaterThan(0);
                    record.DurationMs.Should().BeGreaterThanOrEqualTo(0);
                }
            }
        }

        [TestMethod]
        public void RecordComplete_Disabled_Returns_Without_Update()
        {
            var (handler, cm, tnh) = CreateHandler(enableHistory: false);
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", null, "T", null, null);
                handler.RecordProcessingStart("q1");
                handler.RecordComplete("q1");

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    // RecordComplete should have returned early without updating
                    record.Status.Should().Be((int)MessageHistoryStatus.Processing);
                }
            }
        }

        [TestMethod]
        public void RecordComplete_NoRecord_DoesNotThrow()
        {
            var (handler, cm, _) = CreateHandler();
            using (cm)
            {
                Action act = () => handler.RecordComplete("nonexistent");
                act.Should().NotThrow();
            }
        }

        // === RecordError ===

        [TestMethod]
        public void RecordError_Sets_Error_Status_And_Exception()
        {
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", null, "T", null, null);
                handler.RecordProcessingStart("q1");
                handler.RecordError("q1", "Something went wrong");

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    record.Status.Should().Be((int)MessageHistoryStatus.Error);
                    record.ExceptionText.Should().Be("Something went wrong");
                    record.CompletedUtc.Should().BeGreaterThan(0);
                    record.DurationMs.Should().BeGreaterThanOrEqualTo(0);
                }
            }
        }

        [TestMethod]
        public void RecordError_FromEnqueued_Sets_Error_Without_Duration()
        {
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", null, "T", null, null);
                handler.RecordError("q1", "Error without processing");

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    record.Status.Should().Be((int)MessageHistoryStatus.Error);
                    record.ExceptionText.Should().Be("Error without processing");
                    record.DurationMs.Should().Be(0);
                }
            }
        }

        [TestMethod]
        public void RecordError_NoRecord_DoesNotThrow()
        {
            var (handler, cm, _) = CreateHandler();
            using (cm)
            {
                Action act = () => handler.RecordError("nonexistent", "error");
                act.Should().NotThrow();
            }
        }

        // === RecordRollback ===

        [TestMethod]
        public void RecordRollback_Resets_To_Enqueued_And_Increments_Retry()
        {
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", null, "T", null, null);
                handler.RecordProcessingStart("q1");
                handler.RecordRollback("q1");

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    record.Status.Should().Be((int)MessageHistoryStatus.Enqueued);
                    record.RetryCount.Should().Be(1);
                    record.StartedUtc.Should().Be(0);
                    record.CompletedUtc.Should().Be(0);
                    record.DurationMs.Should().Be(0);
                }
            }
        }

        [TestMethod]
        public void RecordRollback_Multiple_Increments_RetryCount()
        {
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", null, "T", null, null);
                handler.RecordRollback("q1");
                handler.RecordRollback("q1");
                handler.RecordRollback("q1");

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    record.RetryCount.Should().Be(3);
                }
            }
        }

        [TestMethod]
        public void RecordRollback_NoRecord_DoesNotThrow()
        {
            var (handler, cm, _) = CreateHandler();
            using (cm)
            {
                Action act = () => handler.RecordRollback("nonexistent");
                act.Should().NotThrow();
            }
        }

        // === RecordDelete ===

        [TestMethod]
        public void RecordDelete_Sets_Deleted_Status()
        {
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", null, "T", null, null);
                handler.RecordDelete("q1");

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    record.Status.Should().Be((int)MessageHistoryStatus.Deleted);
                    record.CompletedUtc.Should().BeGreaterThan(0);
                }
            }
        }

        [TestMethod]
        public void RecordDelete_NoRecord_DoesNotThrow()
        {
            var (handler, cm, _) = CreateHandler();
            using (cm)
            {
                Action act = () => handler.RecordDelete("nonexistent");
                act.Should().NotThrow();
            }
        }

        // === RecordExpire ===

        [TestMethod]
        public void RecordExpire_Sets_Expired_Status()
        {
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", null, "T", null, null);
                handler.RecordExpire("q1");

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    record.Status.Should().Be((int)MessageHistoryStatus.Expired);
                    record.CompletedUtc.Should().BeGreaterThan(0);
                }
            }
        }

        [TestMethod]
        public void RecordExpire_NoRecord_DoesNotThrow()
        {
            var (handler, cm, _) = CreateHandler();
            using (cm)
            {
                Action act = () => handler.RecordExpire("nonexistent");
                act.Should().NotThrow();
            }
        }

        // === Full lifecycle ===

        [TestMethod]
        public void Full_Lifecycle_Enqueue_Process_Complete()
        {
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", "route1", "MyMessage", null, null);
                handler.RecordProcessingStart("q1");
                handler.RecordComplete("q1");

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    record.Status.Should().Be((int)MessageHistoryStatus.Complete);
                    record.QueueId.Should().Be("q1");
                    record.CorrelationId.Should().Be("c1");
                    record.Route.Should().Be("route1");
                    record.MessageType.Should().Be("MyMessage");
                }
            }
        }
    }
}
