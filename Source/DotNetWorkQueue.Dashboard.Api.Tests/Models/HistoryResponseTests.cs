using System;
using DotNetWorkQueue.Dashboard.Api.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Models
{
    [TestClass]
    public class HistoryResponseTests
    {
        [TestMethod]
        public void QueueId_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { QueueId = "msg-123" };
            Assert.AreEqual("msg-123", sut.QueueId);
        }

        [TestMethod]
        public void CorrelationId_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { CorrelationId = "corr-456" };
            Assert.AreEqual("corr-456", sut.CorrelationId);
        }

        [TestMethod]
        public void Status_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { Status = 3 };
            Assert.AreEqual(3, sut.Status);
        }

        [TestMethod]
        public void EnqueuedUtc_Can_Be_Set_And_Read()
        {
            var time = new DateTime(2026, 3, 24, 12, 0, 0, DateTimeKind.Utc);
            var sut = new HistoryResponse { EnqueuedUtc = time };
            Assert.AreEqual(time, sut.EnqueuedUtc);
        }

        [TestMethod]
        public void DurationMs_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { DurationMs = 1500L };
            Assert.AreEqual(1500L, sut.DurationMs);
        }

        [TestMethod]
        public void ExceptionText_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { ExceptionText = "NullReferenceException" };
            Assert.AreEqual("NullReferenceException", sut.ExceptionText);
        }

        [TestMethod]
        public void RetryCount_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { RetryCount = 3 };
            Assert.AreEqual(3, sut.RetryCount);
        }

        [TestMethod]
        public void Route_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { Route = "high-priority" };
            Assert.AreEqual("high-priority", sut.Route);
        }

        [TestMethod]
        public void All_Properties_Round_Trip()
        {
            var enqueued = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            var started = new DateTime(2026, 1, 15, 10, 30, 1, DateTimeKind.Utc);
            var completed = new DateTime(2026, 1, 15, 10, 30, 2, DateTimeKind.Utc);

            var sut = new HistoryResponse
            {
                QueueId = "q-1",
                CorrelationId = "c-1",
                Status = 2,
                EnqueuedUtc = enqueued,
                StartedUtc = started,
                CompletedUtc = completed,
                DurationMs = 1000L,
                ExceptionText = null,
                RetryCount = 0,
                Route = "default",
                MessageType = "MyApp.Commands.DoWork"
            };

            Assert.AreEqual("q-1", sut.QueueId);
            Assert.AreEqual("c-1", sut.CorrelationId);
            Assert.AreEqual(2, sut.Status);
            Assert.AreEqual(enqueued, sut.EnqueuedUtc);
            Assert.AreEqual(started, sut.StartedUtc);
            Assert.AreEqual(completed, sut.CompletedUtc);
            Assert.AreEqual(1000L, sut.DurationMs);
            Assert.IsNull(sut.ExceptionText);
            Assert.AreEqual(0, sut.RetryCount);
            Assert.AreEqual("default", sut.Route);
            Assert.AreEqual("MyApp.Commands.DoWork", sut.MessageType);
        }
    }
}
