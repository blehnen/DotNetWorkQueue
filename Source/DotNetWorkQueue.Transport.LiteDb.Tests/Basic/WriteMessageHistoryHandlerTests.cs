using System;
using System.Linq;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.Tests.Shared;

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
                    Assert.AreEqual(1, records.Count);
                    Assert.AreEqual("q1", records[0].QueueId);
                    Assert.AreEqual("c1", records[0].CorrelationId);
                    Assert.AreEqual("routeA", records[0].Route);
                    Assert.AreEqual("MyType", records[0].MessageType);
                    Assert.AreEqual((int)MessageHistoryStatus.Enqueued, records[0].Status);
                    Assert.AreEqual(0, records[0].RetryCount);
                    Assert.IsTrue(records[0].EnqueuedUtc > 0);
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
                    AssertHelper.AreEquivalent(body, record.Body);
                    AssertHelper.AreEquivalent(headers, record.Headers);
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
                    Assert.IsNull(record.Body);
                    Assert.IsNull(record.Headers);
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
                    Assert.AreEqual((int)MessageHistoryStatus.Processing, record.Status);
                    Assert.IsTrue(record.StartedUtc > 0);
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
                act();
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
                    Assert.AreEqual((int)MessageHistoryStatus.Complete, record.Status);
                    Assert.IsTrue(record.CompletedUtc > 0);
                    Assert.IsTrue(record.DurationMs >= 0);
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
                    Assert.AreEqual((int)MessageHistoryStatus.Processing, record.Status);
                }
            }
        }

        [TestMethod]
        public void RecordComplete_WithoutProcessingStart_StoresDurationZero()
        {
            var (handler, cm, tnh) = CreateHandler();
            using (cm)
            {
                handler.RecordEnqueue("q1", "c1", null, "T", null, null);
                handler.RecordProcessingStart("q1");

                // Reset StartedUtc to 0 to simulate the race-window where StartedUtc was never persisted
                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    record.StartedUtc = 0;
                    col.Update(record);
                }

                handler.RecordComplete("q1");

                using (var db = cm.GetDatabase())
                {
                    var col = db.Database.GetCollection<HistoryTable>(tnh.HistoryName);
                    var record = col.FindAll().First();
                    Assert.AreEqual((int)MessageHistoryStatus.Complete, record.Status);
                    Assert.AreEqual(0, record.StartedUtc, "StartedUtc was manually cleared to simulate missing start");
                    Assert.AreEqual(0, record.DurationMs, "DurationMs must be explicitly 0 when StartedUtc is not set");
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
                act();
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
                    Assert.AreEqual((int)MessageHistoryStatus.Error, record.Status);
                    Assert.AreEqual("Something went wrong", record.ExceptionText);
                    Assert.IsTrue(record.CompletedUtc > 0);
                    Assert.IsTrue(record.DurationMs >= 0);
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
                    Assert.AreEqual((int)MessageHistoryStatus.Error, record.Status);
                    Assert.AreEqual("Error without processing", record.ExceptionText);
                    Assert.AreEqual(0, record.DurationMs);
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
                act();
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
                    Assert.AreEqual((int)MessageHistoryStatus.Enqueued, record.Status);
                    Assert.AreEqual(1, record.RetryCount);
                    Assert.AreEqual(0, record.StartedUtc);
                    Assert.AreEqual(0, record.CompletedUtc);
                    Assert.AreEqual(0, record.DurationMs);
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
                    Assert.AreEqual(3, record.RetryCount);
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
                act();
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
                    Assert.AreEqual((int)MessageHistoryStatus.Deleted, record.Status);
                    Assert.IsTrue(record.CompletedUtc > 0);
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
                act();
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
                    Assert.AreEqual((int)MessageHistoryStatus.Expired, record.Status);
                    Assert.IsTrue(record.CompletedUtc > 0);
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
                act();
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
                    Assert.AreEqual((int)MessageHistoryStatus.Complete, record.Status);
                    Assert.AreEqual("q1", record.QueueId);
                    Assert.AreEqual("c1", record.CorrelationId);
                    Assert.AreEqual("route1", record.Route);
                    Assert.AreEqual("MyMessage", record.MessageType);
                }
            }
        }
    }
}
