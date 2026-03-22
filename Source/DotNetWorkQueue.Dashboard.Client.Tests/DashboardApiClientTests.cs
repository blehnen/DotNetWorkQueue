using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Client.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Client.Tests
{
    [TestClass]
    public class DashboardApiClientTests
    {
        // === Constructor validation ===

        [TestMethod]
        public void Constructor_Options_Throws_When_Null()
        {
            Action act = () => new DashboardApiClient((DashboardClientOptions)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor_Options_Throws_When_Url_Missing()
        {
            Action act = () => new DashboardApiClient(new DashboardClientOptions());
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Constructor_HttpClient_Throws_When_Null()
        {
            Action act = () => new DashboardApiClient((HttpClient)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor_HttpClientFactory_Throws_When_Null()
        {
            Action act = () => new DashboardApiClient((IHttpClientFactory)null, new DashboardClientOptions { DashboardApiUrl = "http://localhost" });
            act.Should().Throw<ArgumentNullException>();
        }

        // === GetConnectionsAsync ===

        [TestMethod]
        public async Task GetConnectionsAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new[]
            {
                new { Id = Guid.NewGuid(), DisplayName = "Test", QueueCount = 3 }
            });
            using var client = CreateClient(json);

            var result = await client.GetConnectionsAsync();

            result.Success.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value[0].DisplayName.Should().Be("Test");
            result.Value[0].QueueCount.Should().Be(3);
        }

        [TestMethod]
        public async Task GetConnectionsAsync_Returns_Failure_On_Error()
        {
            using var client = CreateClient("Not Found", HttpStatusCode.NotFound);

            var result = await client.GetConnectionsAsync();

            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
            result.ErrorMessage.Should().Be("Not Found");
            result.Value.Should().BeNull();
        }

        // === GetQueueStatusAsync ===

        [TestMethod]
        public async Task GetQueueStatusAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new { Waiting = 10L, Processing = 5L, Error = 2L, Total = 17L });
            using var client = CreateClient(json);

            var result = await client.GetQueueStatusAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Value.Waiting.Should().Be(10);
            result.Value.Total.Should().Be(17);
        }

        // === GetConsumersAsync ===

        [TestMethod]
        public async Task GetConsumersAsync_Returns_Success()
        {
            var consumerId = Guid.NewGuid();
            var json = JsonSerializer.Serialize(new[]
            {
                new { ConsumerId = consumerId, MachineName = "M1", ProcessId = 1234, QueueName = "q" }
            });
            using var client = CreateClient(json);

            var result = await client.GetConsumersAsync();

            result.Success.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value[0].ConsumerId.Should().Be(consumerId);
            result.Value[0].MachineName.Should().Be("M1");
        }

        [TestMethod]
        public async Task GetConsumersAsync_With_QueueId_Passes_Filter()
        {
            string capturedUrl = null;
            var handler = new MockHandler(req =>
            {
                capturedUrl = req.RequestUri.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
                };
            });
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardApiClient(httpClient);

            var queueId = Guid.NewGuid();
            await client.GetConsumersAsync(queueId);

            capturedUrl.Should().Contain($"queueId={queueId}");
        }

        // === GetConsumerCountsAsync ===

        [TestMethod]
        public async Task GetConsumerCountsAsync_Returns_Success()
        {
            var queueId = Guid.NewGuid();
            var json = $"{{\"{queueId}\":3}}";
            using var client = CreateClient(json);

            var result = await client.GetConsumerCountsAsync();

            result.Success.Should().BeTrue();
            result.Value.Should().ContainKey(queueId);
            result.Value[queueId].Should().Be(3);
        }

        // === DeleteMessageAsync returns ApiReturnValue ===

        [TestMethod]
        public async Task DeleteMessageAsync_Returns_Success()
        {
            using var client = CreateClient("", HttpStatusCode.NoContent);

            var result = await client.DeleteMessageAsync(Guid.NewGuid(), "msg-1");

            result.Success.Should().BeTrue();
            result.Value.Should().BeTrue();
        }

        [TestMethod]
        public async Task DeleteMessageAsync_Returns_Failure_On_Error()
        {
            using var client = CreateClient("Not Found", HttpStatusCode.NotFound);

            var result = await client.DeleteMessageAsync(Guid.NewGuid(), "msg-1");

            result.Success.Should().BeFalse();
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // === GetQueuesAsync ===

        [TestMethod]
        public async Task GetQueuesAsync_Returns_Success()
        {
            var id = Guid.NewGuid();
            var json = JsonSerializer.Serialize(new[] { new { Id = id, QueueName = "my-queue" } });
            using var client = CreateClient(json);

            var result = await client.GetQueuesAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value[0].Id.Should().Be(id);
            result.Value[0].QueueName.Should().Be("my-queue");
        }

        // === GetJobsAsync ===

        [TestMethod]
        public async Task GetJobsAsync_Returns_Success()
        {
            var now = DateTimeOffset.UtcNow;
            var json = JsonSerializer.Serialize(new[] { new { JobName = "daily", JobEventTime = now, JobScheduledTime = now.AddHours(1) } });
            using var client = CreateClient(json);

            var result = await client.GetJobsAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value[0].JobName.Should().Be("daily");
            result.Value[0].JobEventTime.Should().Be(now);
            result.Value[0].JobScheduledTime.Should().Be(now.AddHours(1));
        }

        // === GetQueueFeaturesAsync ===

        [TestMethod]
        public async Task GetQueueFeaturesAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new
            {
                EnablePriority = true,
                EnableStatus = true,
                EnableStatusTable = false,
                EnableHeartBeat = true,
                EnableDelayedProcessing = false,
                EnableMessageExpiration = true,
                EnableRoute = false
            });
            using var client = CreateClient(json);

            var result = await client.GetQueueFeaturesAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Value.EnablePriority.Should().BeTrue();
            result.Value.EnableStatus.Should().BeTrue();
            result.Value.EnableStatusTable.Should().BeFalse();
            result.Value.EnableHeartBeat.Should().BeTrue();
            result.Value.EnableDelayedProcessing.Should().BeFalse();
            result.Value.EnableMessageExpiration.Should().BeTrue();
            result.Value.EnableRoute.Should().BeFalse();
        }

        // === GetQueueConfigurationAsync ===

        [TestMethod]
        public async Task GetQueueConfigurationAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new { ConfigurationJson = "{\"heartbeat\":30}" });
            using var client = CreateClient(json);

            var result = await client.GetQueueConfigurationAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Value.ConfigurationJson.Should().Be("{\"heartbeat\":30}");
        }

        // === GetMessagesAsync ===

        [TestMethod]
        public async Task GetMessagesAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new
            {
                Items = new[] { new { QueueId = "msg-1", Status = 0, CorrelationId = "c1" } },
                TotalCount = 50L,
                PageIndex = 0,
                PageSize = 25
            });
            using var client = CreateClient(json);

            var result = await client.GetMessagesAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].QueueId.Should().Be("msg-1");
            result.Value.TotalCount.Should().Be(50);
            result.Value.PageIndex.Should().Be(0);
            result.Value.PageSize.Should().Be(25);
        }

        [TestMethod]
        public async Task GetMessagesAsync_With_Status_Filter_Passes_Parameter()
        {
            string capturedUrl = null;
            var handler = new MockHandler(req =>
            {
                capturedUrl = req.RequestUri.ToString();
                var body = JsonSerializer.Serialize(new { Items = Array.Empty<object>(), TotalCount = 0L, PageIndex = 0, PageSize = 25 });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            });
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardApiClient(httpClient);

            await client.GetMessagesAsync(Guid.NewGuid(), status: 2);

            capturedUrl.Should().Contain("status=2");
        }

        // === GetMessageCountAsync ===

        [TestMethod]
        public async Task GetMessageCountAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(42L);
            using var client = CreateClient(json);

            var result = await client.GetMessageCountAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Value.Should().Be(42);
        }

        [TestMethod]
        public async Task GetMessageCountAsync_With_Status_Passes_Parameter()
        {
            string capturedUrl = null;
            var handler = new MockHandler(req =>
            {
                capturedUrl = req.RequestUri.ToString();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("10", System.Text.Encoding.UTF8, "application/json")
                };
            });
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardApiClient(httpClient);

            await client.GetMessageCountAsync(Guid.NewGuid(), status: 1);

            capturedUrl.Should().Contain("status=1");
        }

        // === GetMessageAsync (detail) ===

        [TestMethod]
        public async Task GetMessageDetailAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new
            {
                QueueId = "msg-5",
                Status = 1,
                CorrelationId = "corr-5",
                Route = "route-b"
            });
            using var client = CreateClient(json);

            var result = await client.GetMessageAsync(Guid.NewGuid(), "msg-5");

            result.Success.Should().BeTrue();
            result.Value.QueueId.Should().Be("msg-5");
            result.Value.Status.Should().Be(1);
            result.Value.CorrelationId.Should().Be("corr-5");
            result.Value.Route.Should().Be("route-b");
        }

        // === GetMessageBodyAsync ===

        [TestMethod]
        public async Task GetMessageBodyAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new
            {
                Body = "{\"data\":1}",
                TypeName = "MyMessage",
                IsEditable = true,
                IsProcessing = false
            });
            using var client = CreateClient(json);

            var result = await client.GetMessageBodyAsync(Guid.NewGuid(), "msg-1");

            result.Success.Should().BeTrue();
            result.Value.Body.Should().Be("{\"data\":1}");
            result.Value.TypeName.Should().Be("MyMessage");
            result.Value.IsEditable.Should().BeTrue();
            result.Value.IsProcessing.Should().BeFalse();
        }

        // === GetMessageHeadersAsync ===

        [TestMethod]
        public async Task GetMessageHeadersAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new
            {
                Headers = new Dictionary<string, string> { { "key1", "val1" }, { "key2", "val2" } }
            });
            using var client = CreateClient(json);

            var result = await client.GetMessageHeadersAsync(Guid.NewGuid(), "msg-1");

            result.Success.Should().BeTrue();
            result.Value.Headers.Should().HaveCount(2);
            result.Value.Headers["key1"].Should().Be("val1");
        }

        // === GetErrorRetriesAsync ===

        [TestMethod]
        public async Task GetErrorRetriesAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new[]
            {
                new { ErrorTrackingId = 1L, QueueId = "msg-1", ExceptionType = "System.Exception", RetryCount = 3 }
            });
            using var client = CreateClient(json);

            var result = await client.GetErrorRetriesAsync(Guid.NewGuid(), "msg-1");

            result.Success.Should().BeTrue();
            result.Value.Should().HaveCount(1);
            result.Value[0].ErrorTrackingId.Should().Be(1);
            result.Value[0].ExceptionType.Should().Be("System.Exception");
            result.Value[0].RetryCount.Should().Be(3);
        }

        // === GetStaleMessagesAsync ===

        [TestMethod]
        public async Task GetStaleMessagesAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new
            {
                Items = new[] { new { QueueId = "stale-1", Status = 1 } },
                TotalCount = 5L,
                PageIndex = 0,
                PageSize = 25
            });
            using var client = CreateClient(json);

            var result = await client.GetStaleMessagesAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].QueueId.Should().Be("stale-1");
            result.Value.TotalCount.Should().Be(5);
        }

        // === GetErrorsAsync ===

        [TestMethod]
        public async Task GetErrorsAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new
            {
                Items = new[] { new { Id = 1L, QueueId = "msg-1", LastException = "boom", LastExceptionDate = DateTimeOffset.UtcNow } },
                TotalCount = 10L,
                PageIndex = 0,
                PageSize = 25
            });
            using var client = CreateClient(json);

            var result = await client.GetErrorsAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Value.Items.Should().HaveCount(1);
            result.Value.Items[0].QueueId.Should().Be("msg-1");
            result.Value.Items[0].LastException.Should().Be("boom");
            result.Value.TotalCount.Should().Be(10);
        }

        // === RequeueErrorMessageAsync ===

        [TestMethod]
        public async Task RequeueErrorMessageAsync_Returns_Success()
        {
            using var client = CreateClient("", HttpStatusCode.NoContent);

            var result = await client.RequeueErrorMessageAsync(Guid.NewGuid(), "msg-1");

            result.Success.Should().BeTrue();
            result.Value.Should().BeTrue();
        }

        // === ResetMessageAsync (stale) ===

        [TestMethod]
        public async Task ResetStaleMessageAsync_Returns_Success()
        {
            using var client = CreateClient("", HttpStatusCode.NoContent);

            var result = await client.ResetMessageAsync(Guid.NewGuid(), "msg-1");

            result.Success.Should().BeTrue();
            result.Value.Should().BeTrue();
        }

        // === EditMessageBodyAsync ===

        [TestMethod]
        public async Task EditMessageBodyAsync_Returns_Success()
        {
            using var client = CreateClient("", HttpStatusCode.NoContent);

            var result = await client.EditMessageBodyAsync(Guid.NewGuid(), "msg-1", "{\"updated\":true}");

            result.Success.Should().BeTrue();
            result.Value.Should().BeTrue();
        }

        // === RequeueAllErrorsAsync ===

        [TestMethod]
        public async Task RequeueAllErrorsAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new { Count = 7L });
            using var client = CreateClient(json);

            var result = await client.RequeueAllErrorsAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Value.Count.Should().Be(7);
        }

        // === ResetAllStaleMessagesAsync ===

        [TestMethod]
        public async Task ResetAllStaleMessagesAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new { Count = 3L });
            using var client = CreateClient(json);

            var result = await client.ResetAllStaleMessagesAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Value.Count.Should().Be(3);
        }

        // === DeleteAllErrorsAsync ===

        [TestMethod]
        public async Task DeleteAllErrorsAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new { Deleted = 12L });
            using var client = CreateClient(json);

            var result = await client.DeleteAllErrorsAsync(Guid.NewGuid());

            result.Success.Should().BeTrue();
            result.Value.Deleted.Should().Be(12);
        }

        // === Dispose ===

        [TestMethod]
        public void Dispose_Is_Safe_When_Owns_Client()
        {
            var opts = new DashboardClientOptions { DashboardApiUrl = "http://localhost:5000" };
            var client = new DashboardApiClient(opts);
            client.Dispose();
            // No exception = success
        }

        [TestMethod]
        public void Dispose_Does_Not_Dispose_External_Client()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            var client = new DashboardApiClient(httpClient);
            client.Dispose();

            // External client should still be usable (not disposed)
            httpClient.BaseAddress.Should().NotBeNull();
        }

        // === Helpers ===

        private static DashboardApiClient CreateClient(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            });
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            return new DashboardApiClient(httpClient);
        }

        private class MockHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

            public MockHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_handler(request));
            }
        }
    }
}
