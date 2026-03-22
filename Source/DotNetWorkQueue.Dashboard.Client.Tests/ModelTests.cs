using System;
using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Client.Models;
using FluentAssertions;
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

            sut.QueueId.Should().Be("msg-1");
            sut.QueuedDateTime.Should().Be(now);
            sut.CorrelationId.Should().Be("corr-1");
            sut.Status.Should().Be(1);
            sut.Priority.Should().Be(5);
            sut.QueueProcessTime.Should().Be(now.AddMinutes(1));
            sut.HeartBeat.Should().Be(now.AddMinutes(2));
            sut.ExpirationTime.Should().Be(now.AddMinutes(3));
            sut.Route.Should().Be("route-a");
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

            sut.Id.Should().Be(42);
            sut.QueueId.Should().Be("msg-2");
            sut.LastException.Should().Be("NullRef");
            sut.LastExceptionDate.Should().Be(now);
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

            sut.EnablePriority.Should().BeTrue();
            sut.EnableStatus.Should().BeTrue();
            sut.EnableStatusTable.Should().BeTrue();
            sut.EnableHeartBeat.Should().BeTrue();
            sut.EnableDelayedProcessing.Should().BeTrue();
            sut.EnableMessageExpiration.Should().BeTrue();
            sut.EnableRoute.Should().BeTrue();
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

            sut.Id.Should().Be(id);
            sut.QueueName.Should().Be("test-queue");
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

            sut.Waiting.Should().Be(10);
            sut.Processing.Should().Be(5);
            sut.Error.Should().Be(2);
            sut.Total.Should().Be(17);
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

            sut.JobName.Should().Be("daily-cleanup");
            sut.JobEventTime.Should().Be(now);
            sut.JobScheduledTime.Should().Be(now.AddHours(1));
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

            sut.Body.Should().Be("{\"key\":\"value\"}");
            sut.TypeName.Should().Be("MyType");
            sut.IsEditable.Should().BeTrue();
            sut.IsProcessing.Should().BeFalse();
        }

        [TestMethod]
        public void MessageHeadersResponse_Properties()
        {
            var headers = new Dictionary<string, string> { { "h1", "v1" }, { "h2", "v2" } };
            var sut = new MessageHeadersResponse
            {
                Headers = headers
            };

            sut.Headers.Should().HaveCount(2);
            sut.Headers["h1"].Should().Be("v1");
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

            sut.ErrorTrackingId.Should().Be(99);
            sut.QueueId.Should().Be("msg-5");
            sut.ExceptionType.Should().Be("System.InvalidOperationException");
            sut.RetryCount.Should().Be(3);
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

            sut.Items.Should().HaveCount(2);
            sut.TotalCount.Should().Be(100);
            sut.PageIndex.Should().Be(2);
            sut.PageSize.Should().Be(25);
        }

        [TestMethod]
        public void BulkActionResponse_Properties()
        {
            var sut = new BulkActionResponse { Count = 42 };
            sut.Count.Should().Be(42);
        }

        [TestMethod]
        public void DeleteAllResponse_Properties()
        {
            var sut = new DeleteAllResponse { Deleted = 15 };
            sut.Deleted.Should().Be(15);
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

            sut.ConsumerId.Should().Be(id);
            sut.HeartbeatIntervalSeconds.Should().Be(30);
        }

        [TestMethod]
        public void EditMessageBodyRequest_Properties()
        {
            var sut = new EditMessageBodyRequest { Body = "new body" };
            sut.Body.Should().Be("new body");
        }

        [TestMethod]
        public void ConfigurationResponse_Properties()
        {
            var sut = new ConfigurationResponse { ConfigurationJson = "{\"setting\":true}" };
            sut.ConfigurationJson.Should().Be("{\"setting\":true}");
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

            sut.ConsumerId.Should().Be(consumerId);
            sut.QueueName.Should().Be("q1");
            sut.MachineName.Should().Be("SERVER1");
            sut.ProcessId.Should().Be(9876);
            sut.FriendlyName.Should().Be("Worker-1");
            sut.RegisteredAt.Should().Be(now);
            sut.LastHeartbeat.Should().Be(now.AddSeconds(10));
            sut.MatchedQueueId.Should().Be(queueId);
            sut.MessagesProcessed.Should().Be(100);
            sut.MessagesErrored.Should().Be(5);
            sut.MessagesRolledBack.Should().Be(2);
            sut.PoisonMessages.Should().Be(1);
        }
    }
}
