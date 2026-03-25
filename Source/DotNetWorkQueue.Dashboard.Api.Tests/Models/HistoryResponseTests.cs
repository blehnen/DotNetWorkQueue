using System;
using DotNetWorkQueue.Dashboard.Api.Models;
using FluentAssertions;
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
            sut.QueueId.Should().Be("msg-123");
        }

        [TestMethod]
        public void CorrelationId_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { CorrelationId = "corr-456" };
            sut.CorrelationId.Should().Be("corr-456");
        }

        [TestMethod]
        public void Status_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { Status = 3 };
            sut.Status.Should().Be(3);
        }

        [TestMethod]
        public void EnqueuedUtc_Can_Be_Set_And_Read()
        {
            var time = new DateTime(2026, 3, 24, 12, 0, 0, DateTimeKind.Utc);
            var sut = new HistoryResponse { EnqueuedUtc = time };
            sut.EnqueuedUtc.Should().Be(time);
        }

        [TestMethod]
        public void DurationMs_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { DurationMs = 1500L };
            sut.DurationMs.Should().Be(1500L);
        }

        [TestMethod]
        public void ExceptionText_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { ExceptionText = "NullReferenceException" };
            sut.ExceptionText.Should().Be("NullReferenceException");
        }

        [TestMethod]
        public void RetryCount_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { RetryCount = 3 };
            sut.RetryCount.Should().Be(3);
        }

        [TestMethod]
        public void Route_Can_Be_Set_And_Read()
        {
            var sut = new HistoryResponse { Route = "high-priority" };
            sut.Route.Should().Be("high-priority");
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

            sut.QueueId.Should().Be("q-1");
            sut.CorrelationId.Should().Be("c-1");
            sut.Status.Should().Be(2);
            sut.EnqueuedUtc.Should().Be(enqueued);
            sut.StartedUtc.Should().Be(started);
            sut.CompletedUtc.Should().Be(completed);
            sut.DurationMs.Should().Be(1000L);
            sut.ExceptionText.Should().BeNull();
            sut.RetryCount.Should().Be(0);
            sut.Route.Should().Be("default");
            sut.MessageType.Should().Be("MyApp.Commands.DoWork");
        }
    }
}
