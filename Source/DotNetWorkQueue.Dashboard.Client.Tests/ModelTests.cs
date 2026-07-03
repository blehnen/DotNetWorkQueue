using System;
using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Client.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Client.Tests
{
    [TestClass]
    public class ModelTests
    {
        [TestMethod]
        public void MessageResponse_Properties()
        {
            var now = DateTimeOffset.UtcNow;
            var sut = new MessageResponse
            {
                QueueId = "msg-1",
                QueuedDateTime = now,
                CorrelationId = "corr-1",
                Status = 1,
                Priority = 5,
                QueueProcessTime = now.AddMinutes(1),
                HeartBeat = now.AddMinutes(2),
                ExpirationTime = now.AddMinutes(3),
                Route = "route-a"
            };

            Assert.AreEqual("msg-1", sut.QueueId);
            Assert.AreEqual(now, sut.QueuedDateTime);
            Assert.AreEqual("corr-1", sut.CorrelationId);
            Assert.AreEqual(1, sut.Status);
            Assert.AreEqual(5, sut.Priority);
            Assert.AreEqual(now.AddMinutes(1), sut.QueueProcessTime);
            Assert.AreEqual(now.AddMinutes(2), sut.HeartBeat);
            Assert.AreEqual(now.AddMinutes(3), sut.ExpirationTime);
            Assert.AreEqual("route-a", sut.Route);
        }

        [TestMethod]
        public void ErrorMessageResponse_Properties()
        {
            var now = DateTimeOffset.UtcNow;
            var sut = new ErrorMessageResponse
            {
                Id = 42,
                QueueId = "msg-2",
                LastException = "NullRef",
                LastExceptionDate = now
            };

            Assert.AreEqual(42, sut.Id);
            Assert.AreEqual("msg-2", sut.QueueId);
            Assert.AreEqual("NullRef", sut.LastException);
            Assert.AreEqual(now, sut.LastExceptionDate);
        }

        [TestMethod]
        public void QueueFeaturesResponse_Properties()
        {
            var sut = new QueueFeaturesResponse
            {
                EnablePriority = true,
                EnableStatus = true,
                EnableStatusTable = true,
                EnableHeartBeat = true,
                EnableDelayedProcessing = true,
                EnableMessageExpiration = true,
                EnableRoute = true
            };

            Assert.IsTrue(sut.EnablePriority);
            Assert.IsTrue(sut.EnableStatus);
            Assert.IsTrue(sut.EnableStatusTable);
            Assert.IsTrue(sut.EnableHeartBeat);
            Assert.IsTrue(sut.EnableDelayedProcessing);
            Assert.IsTrue(sut.EnableMessageExpiration);
            Assert.IsTrue(sut.EnableRoute);
        }

        [TestMethod]
        public void QueueInfoResponse_Properties()
        {
            var id = Guid.NewGuid();
            var sut = new QueueInfoResponse
            {
                Id = id,
                QueueName = "test-queue"
            };

            Assert.AreEqual(id, sut.Id);
            Assert.AreEqual("test-queue", sut.QueueName);
        }

        [TestMethod]
        public void QueueStatusResponse_Properties()
        {
            var sut = new QueueStatusResponse
            {
                Waiting = 10,
                Processing = 5,
                Error = 2,
                Total = 17
            };

            Assert.AreEqual(10, sut.Waiting);
            Assert.AreEqual(5, sut.Processing);
            Assert.AreEqual(2, sut.Error);
            Assert.AreEqual(17, sut.Total);
        }

        [TestMethod]
        public void JobResponse_Properties()
        {
            var now = DateTimeOffset.UtcNow;
            var sut = new JobResponse
            {
                JobName = "daily-cleanup",
                JobEventTime = now,
                JobScheduledTime = now.AddHours(1)
            };

            Assert.AreEqual("daily-cleanup", sut.JobName);
            Assert.AreEqual(now, sut.JobEventTime);
            Assert.AreEqual(now.AddHours(1), sut.JobScheduledTime);
        }

        [TestMethod]
        public void MessageBodyResponse_Properties()
        {
            var sut = new MessageBodyResponse
            {
                Body = "{\"key\":\"value\"}",
                TypeName = "MyType",
                IsEditable = true,
                IsProcessing = false
            };

            Assert.AreEqual("{\"key\":\"value\"}", sut.Body);
            Assert.AreEqual("MyType", sut.TypeName);
            Assert.IsTrue(sut.IsEditable);
            Assert.IsFalse(sut.IsProcessing);
        }

        [TestMethod]
        public void MessageHeadersResponse_Properties()
        {
            var headers = new Dictionary<string, string> { { "h1", "v1" }, { "h2", "v2" } };
            var sut = new MessageHeadersResponse
            {
                Headers = headers
            };

            Assert.AreEqual(2, sut.Headers.Count);
            Assert.AreEqual("v1", sut.Headers["h1"]);
        }

        [TestMethod]
        public void ErrorRetryResponse_Properties()
        {
            var sut = new ErrorRetryResponse
            {
                ErrorTrackingId = 99,
                QueueId = "msg-5",
                ExceptionType = "System.InvalidOperationException",
                RetryCount = 3
            };

            Assert.AreEqual(99, sut.ErrorTrackingId);
            Assert.AreEqual("msg-5", sut.QueueId);
            Assert.AreEqual("System.InvalidOperationException", sut.ExceptionType);
            Assert.AreEqual(3, sut.RetryCount);
        }

        [TestMethod]
        public void PagedResponse_Properties()
        {
            var sut = new PagedResponse<string>
            {
                Items = new List<string> { "a", "b" },
                TotalCount = 100,
                PageIndex = 2,
                PageSize = 25
            };

            Assert.AreEqual(2, sut.Items.Count);
            Assert.AreEqual(100, sut.TotalCount);
            Assert.AreEqual(2, sut.PageIndex);
            Assert.AreEqual(25, sut.PageSize);
        }

        [TestMethod]
        public void BulkActionResponse_Properties()
        {
            var sut = new BulkActionResponse { Count = 42 };
            Assert.AreEqual(42, sut.Count);
        }

        [TestMethod]
        public void DeleteAllResponse_Properties()
        {
            var sut = new DeleteAllResponse { Deleted = 15 };
            Assert.AreEqual(15, sut.Deleted);
        }

        [TestMethod]
        public void ConsumerRegistrationResponse_Properties()
        {
            var id = Guid.NewGuid();
            var sut = new ConsumerRegistrationResponse
            {
                ConsumerId = id,
                HeartbeatIntervalSeconds = 30
            };

            Assert.AreEqual(id, sut.ConsumerId);
            Assert.AreEqual(30, sut.HeartbeatIntervalSeconds);
        }

        [TestMethod]
        public void EditMessageBodyRequest_Properties()
        {
            var sut = new EditMessageBodyRequest { Body = "new body" };
            Assert.AreEqual("new body", sut.Body);
        }

        [TestMethod]
        public void ConfigurationResponse_Properties()
        {
            var sut = new ConfigurationResponse { ConfigurationJson = "{\"setting\":true}" };
            Assert.AreEqual("{\"setting\":true}", sut.ConfigurationJson);
        }

        [TestMethod]
        public void ConsumerInfoResponse_Properties()
        {
            var consumerId = Guid.NewGuid();
            var queueId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var sut = new ConsumerInfoResponse
            {
                ConsumerId = consumerId,
                QueueName = "q1",
                MachineName = "SERVER1",
                ProcessId = 9876,
                FriendlyName = "Worker-1",
                RegisteredAt = now,
                LastHeartbeat = now.AddSeconds(10),
                MatchedQueueId = queueId,
                MessagesProcessed = 100,
                MessagesErrored = 5,
                MessagesRolledBack = 2,
                PoisonMessages = 1
            };

            Assert.AreEqual(consumerId, sut.ConsumerId);
            Assert.AreEqual("q1", sut.QueueName);
            Assert.AreEqual("SERVER1", sut.MachineName);
            Assert.AreEqual(9876, sut.ProcessId);
            Assert.AreEqual("Worker-1", sut.FriendlyName);
            Assert.AreEqual(now, sut.RegisteredAt);
            Assert.AreEqual(now.AddSeconds(10), sut.LastHeartbeat);
            Assert.AreEqual(queueId, sut.MatchedQueueId);
            Assert.AreEqual(100, sut.MessagesProcessed);
            Assert.AreEqual(5, sut.MessagesErrored);
            Assert.AreEqual(2, sut.MessagesRolledBack);
            Assert.AreEqual(1, sut.PoisonMessages);
        }
    }
}
