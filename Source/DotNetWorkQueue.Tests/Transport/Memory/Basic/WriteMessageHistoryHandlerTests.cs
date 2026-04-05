using System;
using System.Text;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Memory.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic
{
    [TestClass]
    public class WriteMessageHistoryHandlerTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var handler = CreateHandler();
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void RecordEnqueue_HistoryDisabled_DoesNotStore()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: false);
            handler.RecordEnqueue("q1", "c1", "route", "MyType", new byte[] { 1 }, new byte[] { 2 });

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            Assert.IsNull(records);
        }

        [TestMethod]
        public void RecordEnqueue_HistoryEnabled_StoresRecord()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordEnqueue("q1", "c1", "route", "MyType", new byte[] { 1 }, new byte[] { 2 });

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            Assert.IsNotNull(records);
            Assert.IsTrue(records.ContainsKey("q1"));

            var record = records["q1"];
            Assert.AreEqual("q1", record.QueueId);
            Assert.AreEqual("c1", record.CorrelationId);
            Assert.AreEqual(MessageHistoryStatus.Enqueued, record.Status);
            Assert.AreEqual("route", record.Route);
            Assert.AreEqual("MyType", record.MessageType);
            Assert.AreEqual(0, record.RetryCount);
            Assert.IsTrue(record.EnqueuedUtc <= DateTime.UtcNow);
        }

        [TestMethod]
        public void RecordEnqueue_StoreBodyEnabled_StoresBodyAndHeaders()
        {
            var body = Encoding.UTF8.GetBytes("test body");
            var headers = Encoding.UTF8.GetBytes("test headers");
            var (handler, key) = CreateHandlerWithKey(enableHistory: true, storeBody: true);

            handler.RecordEnqueue("q1", "c1", null, null, body, headers);

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            CollectionAssert.AreEqual(body, record.Body);
            CollectionAssert.AreEqual(headers, record.Headers);
        }

        [TestMethod]
        public void RecordEnqueue_StoreBodyDisabled_DoesNotStoreBodyOrHeaders()
        {
            var body = Encoding.UTF8.GetBytes("test body");
            var headers = Encoding.UTF8.GetBytes("test headers");
            var (handler, key) = CreateHandlerWithKey(enableHistory: true, storeBody: false);

            handler.RecordEnqueue("q1", "c1", null, null, body, headers);

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.IsNull(record.Body);
            Assert.IsNull(record.Headers);
        }

        [TestMethod]
        public void RecordProcessingStart_HistoryDisabled_DoesNothing()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: false);
            handler.RecordProcessingStart("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            Assert.IsNull(records);
        }

        [TestMethod]
        public void RecordProcessingStart_HistoryEnabled_UpdatesStatus()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordEnqueue("q1", "c1", null, null, null, null);

            handler.RecordProcessingStart("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.AreEqual(MessageHistoryStatus.Processing, record.Status);
            Assert.IsNotNull(record.StartedUtc);
            Assert.IsTrue(record.StartedUtc.Value <= DateTime.UtcNow);
        }

        [TestMethod]
        public void RecordProcessingStart_RecordNotFound_DoesNotThrow()
        {
            var (handler, _) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordProcessingStart("nonexistent");
            // No exception thrown
        }

        [TestMethod]
        public void RecordComplete_HistoryDisabled_DoesNothing()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: false);
            handler.RecordComplete("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            Assert.IsNull(records);
        }

        [TestMethod]
        public void RecordComplete_HistoryEnabled_UpdatesStatusAndCompletedUtc()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordEnqueue("q1", "c1", null, null, null, null);
            handler.RecordProcessingStart("q1");

            handler.RecordComplete("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.AreEqual(MessageHistoryStatus.Complete, record.Status);
            Assert.IsNotNull(record.CompletedUtc);
            Assert.IsNotNull(record.DurationMs);
            Assert.IsTrue(record.DurationMs.Value >= 0);
        }

        [TestMethod]
        public void RecordComplete_WithoutStarted_DurationIsZero()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordEnqueue("q1", "c1", null, null, null, null);

            // Complete without starting - StartedUtc is null
            handler.RecordComplete("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.AreEqual(MessageHistoryStatus.Complete, record.Status);
            Assert.IsNotNull(record.CompletedUtc);
            Assert.AreEqual(0L, record.DurationMs);
        }

        [TestMethod]
        public void RecordComplete_RecordNotFound_DoesNotThrow()
        {
            var (handler, _) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordComplete("nonexistent");
        }

        [TestMethod]
        public void RecordError_HistoryDisabled_DoesNothing()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: false);
            handler.RecordError("q1", "something went wrong");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            Assert.IsNull(records);
        }

        [TestMethod]
        public void RecordError_HistoryEnabled_UpdatesStatusAndException()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordEnqueue("q1", "c1", null, null, null, null);
            handler.RecordProcessingStart("q1");

            handler.RecordError("q1", "NullReferenceException");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.AreEqual(MessageHistoryStatus.Error, record.Status);
            Assert.AreEqual("NullReferenceException", record.ExceptionText);
            Assert.IsNotNull(record.CompletedUtc);
            Assert.IsNotNull(record.DurationMs);
            Assert.IsTrue(record.DurationMs.Value >= 0);
        }

        [TestMethod]
        public void RecordError_WithoutStarted_DurationIsZero()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordEnqueue("q1", "c1", null, null, null, null);

            handler.RecordError("q1", "error");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.AreEqual(MessageHistoryStatus.Error, record.Status);
            Assert.AreEqual(0L, record.DurationMs);
        }

        [TestMethod]
        public void RecordError_RecordNotFound_DoesNotThrow()
        {
            var (handler, _) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordError("nonexistent", "error");
        }

        [TestMethod]
        public void RecordRollback_HistoryDisabled_DoesNothing()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: false);
            handler.RecordRollback("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            Assert.IsNull(records);
        }

        [TestMethod]
        public void RecordRollback_HistoryEnabled_ResetsToEnqueuedAndIncrementsRetry()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordEnqueue("q1", "c1", null, null, null, null);
            handler.RecordProcessingStart("q1");

            handler.RecordRollback("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.AreEqual(MessageHistoryStatus.Enqueued, record.Status);
            Assert.AreEqual(1, record.RetryCount);
            Assert.IsNull(record.StartedUtc);
            Assert.IsNull(record.CompletedUtc);
            Assert.IsNull(record.DurationMs);
        }

        [TestMethod]
        public void RecordRollback_MultipleTimes_IncrementsRetryCount()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordEnqueue("q1", "c1", null, null, null, null);

            handler.RecordProcessingStart("q1");
            handler.RecordRollback("q1");

            handler.RecordProcessingStart("q1");
            handler.RecordRollback("q1");

            handler.RecordProcessingStart("q1");
            handler.RecordRollback("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.AreEqual(3, record.RetryCount);
            Assert.AreEqual(MessageHistoryStatus.Enqueued, record.Status);
        }

        [TestMethod]
        public void RecordRollback_RecordNotFound_DoesNotThrow()
        {
            var (handler, _) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordRollback("nonexistent");
        }

        [TestMethod]
        public void RecordDelete_HistoryDisabled_DoesNothing()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: false);
            handler.RecordDelete("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            Assert.IsNull(records);
        }

        [TestMethod]
        public void RecordDelete_HistoryEnabled_SetsDeletedStatus()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordEnqueue("q1", "c1", null, null, null, null);

            handler.RecordDelete("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.AreEqual(MessageHistoryStatus.Deleted, record.Status);
            Assert.IsNotNull(record.CompletedUtc);
        }

        [TestMethod]
        public void RecordDelete_RecordNotFound_DoesNotThrow()
        {
            var (handler, _) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordDelete("nonexistent");
        }

        [TestMethod]
        public void RecordExpire_HistoryDisabled_DoesNothing()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: false);
            handler.RecordExpire("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            Assert.IsNull(records);
        }

        [TestMethod]
        public void RecordExpire_HistoryEnabled_SetsExpiredStatus()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordEnqueue("q1", "c1", null, null, null, null);

            handler.RecordExpire("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.AreEqual(MessageHistoryStatus.Expired, record.Status);
            Assert.IsNotNull(record.CompletedUtc);
        }

        [TestMethod]
        public void RecordExpire_RecordNotFound_DoesNotThrow()
        {
            var (handler, _) = CreateHandlerWithKey(enableHistory: true);
            handler.RecordExpire("nonexistent");
        }

        [TestMethod]
        public void FullLifecycle_Enqueue_Process_Complete()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);

            handler.RecordEnqueue("q1", "corr1", "routeA", "MyMessage", null, null);
            handler.RecordProcessingStart("q1");
            handler.RecordComplete("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.AreEqual(MessageHistoryStatus.Complete, record.Status);
            Assert.AreEqual("corr1", record.CorrelationId);
            Assert.AreEqual("routeA", record.Route);
            Assert.AreEqual("MyMessage", record.MessageType);
            Assert.IsTrue(record.EnqueuedUtc > DateTime.MinValue);
            Assert.IsNotNull(record.StartedUtc);
            Assert.IsNotNull(record.CompletedUtc);
            Assert.IsNotNull(record.DurationMs);
        }

        [TestMethod]
        public void FullLifecycle_Enqueue_Process_Error()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);

            handler.RecordEnqueue("q1", "corr1", null, null, null, null);
            handler.RecordProcessingStart("q1");
            handler.RecordError("q1", "Something failed");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.AreEqual(MessageHistoryStatus.Error, record.Status);
            Assert.AreEqual("Something failed", record.ExceptionText);
        }

        [TestMethod]
        public void FullLifecycle_Enqueue_Process_Rollback_Process_Complete()
        {
            var (handler, key) = CreateHandlerWithKey(enableHistory: true);

            handler.RecordEnqueue("q1", "corr1", null, null, null, null);
            handler.RecordProcessingStart("q1");
            handler.RecordRollback("q1");

            var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
            var record = records["q1"];
            Assert.AreEqual(1, record.RetryCount);
            Assert.AreEqual(MessageHistoryStatus.Enqueued, record.Status);

            handler.RecordProcessingStart("q1");
            handler.RecordComplete("q1");

            record = records["q1"];
            Assert.AreEqual(MessageHistoryStatus.Complete, record.Status);
            Assert.AreEqual(1, record.RetryCount);
        }

        private static WriteMessageHistoryHandler CreateHandler()
        {
            var connectionInfo = Substitute.For<IConnectionInformation>();
            connectionInfo.QueueName.Returns("TestQueue");
            connectionInfo.ConnectionString.Returns("TestConnection");

            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(false);

            return new WriteMessageHistoryHandler(connectionInfo, options);
        }

        private static (WriteMessageHistoryHandler handler, string key) CreateHandlerWithKey(
            bool enableHistory = true, bool storeBody = false)
        {
            // Use unique names to avoid static dictionary collisions between tests
            var queueName = $"TestQueue_{Guid.NewGuid():N}";
            var connectionString = $"TestConn_{Guid.NewGuid():N}";

            var connectionInfo = Substitute.For<IConnectionInformation>();
            connectionInfo.QueueName.Returns(queueName);
            connectionInfo.ConnectionString.Returns(connectionString);

            var historyOptions = Substitute.For<IHistoryTransportOptions>();
            historyOptions.StoreBody.Returns(storeBody);

            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(enableHistory);
            options.HistoryOptions.Returns(historyOptions);

            var handler = new WriteMessageHistoryHandler(connectionInfo, options);
            var key = $"{queueName}|{connectionString}";
            return (handler, key);
        }
    }
}
