using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Client.Models;
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
            Assert.Throws<ArgumentNullException>(act);
        }

        [TestMethod]
        public void Constructor_Options_Throws_When_Url_Missing()
        {
            Action act = () => new DashboardApiClient(new DashboardClientOptions());
            Assert.Throws<ArgumentException>(act);
        }

        [TestMethod]
        public void Constructor_HttpClient_Throws_When_Null()
        {
            Action act = () => new DashboardApiClient((HttpClient)null);
            Assert.Throws<ArgumentNullException>(act);
        }

        [TestMethod]
        public void Constructor_HttpClientFactory_Throws_When_Null()
        {
            Action act = () => new DashboardApiClient((IHttpClientFactory)null, new DashboardClientOptions { DashboardApiUrl = "http://localhost" });
            Assert.Throws<ArgumentNullException>(act);
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

            Assert.IsTrue(result.Success);
            Assert.HasCount(1, result.Value);
            Assert.AreEqual("Test", result.Value[0].DisplayName);
            Assert.AreEqual(3, result.Value[0].QueueCount);
        }

        [TestMethod]
        public async Task GetConnectionsAsync_Returns_Failure_On_Error()
        {
            using var client = CreateClient("Not Found", HttpStatusCode.NotFound);

            var result = await client.GetConnectionsAsync();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
            Assert.AreEqual("Not Found", result.ErrorMessage);
            Assert.IsNull(result.Value);
        }

        // === GetQueueStatusAsync ===

        [TestMethod]
        public async Task GetQueueStatusAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new { Waiting = 10L, Processing = 5L, Error = 2L, Total = 17L });
            using var client = CreateClient(json);

            var result = await client.GetQueueStatusAsync(Guid.NewGuid());

            Assert.IsTrue(result.Success);
            Assert.AreEqual(10, result.Value.Waiting);
            Assert.AreEqual(17, result.Value.Total);
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

            Assert.IsTrue(result.Success);
            Assert.HasCount(1, result.Value);
            Assert.AreEqual(consumerId, result.Value[0].ConsumerId);
            Assert.AreEqual("M1", result.Value[0].MachineName);
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

            StringAssert.Contains(capturedUrl, $"queueId={queueId}");
        }

        // === GetConsumerCountsAsync ===

        [TestMethod]
        public async Task GetConsumerCountsAsync_Returns_Success()
        {
            var queueId = Guid.NewGuid();
            var json = $"{{\"{queueId}\":3}}";
            using var client = CreateClient(json);

            var result = await client.GetConsumerCountsAsync();

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.Value.ContainsKey(queueId));
            Assert.AreEqual(3, result.Value[queueId]);
        }

        // === DeleteMessageAsync returns ApiReturnValue ===

        [TestMethod]
        public async Task DeleteMessageAsync_Returns_Success()
        {
            using var client = CreateClient("", HttpStatusCode.NoContent);

            var result = await client.DeleteMessageAsync(Guid.NewGuid(), "msg-1");

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.Value);
        }

        [TestMethod]
        public async Task DeleteMessageAsync_Returns_Failure_On_Error()
        {
            using var client = CreateClient("Not Found", HttpStatusCode.NotFound);

            var result = await client.DeleteMessageAsync(Guid.NewGuid(), "msg-1");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode);
        }

        // === GetQueuesAsync ===

        [TestMethod]
        public async Task GetQueuesAsync_Returns_Success()
        {
            var id = Guid.NewGuid();
            var json = JsonSerializer.Serialize(new[] { new { Id = id, QueueName = "my-queue" } });
            using var client = CreateClient(json);

            var result = await client.GetQueuesAsync(Guid.NewGuid());

            Assert.IsTrue(result.Success);
            Assert.HasCount(1, result.Value);
            Assert.AreEqual(id, result.Value[0].Id);
            Assert.AreEqual("my-queue", result.Value[0].QueueName);
        }

        // === GetJobsAsync ===

        [TestMethod]
        public async Task GetJobsAsync_Returns_Success()
        {
            var now = DateTimeOffset.UtcNow;
            var json = JsonSerializer.Serialize(new[] { new { JobName = "daily", JobEventTime = now, JobScheduledTime = now.AddHours(1) } });
            using var client = CreateClient(json);

            var result = await client.GetJobsAsync(Guid.NewGuid());

            Assert.IsTrue(result.Success);
            Assert.HasCount(1, result.Value);
            Assert.AreEqual("daily", result.Value[0].JobName);
            Assert.AreEqual(now, result.Value[0].JobEventTime);
            Assert.AreEqual(now.AddHours(1), result.Value[0].JobScheduledTime);
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

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.Value.EnablePriority);
            Assert.IsTrue(result.Value.EnableStatus);
            Assert.IsFalse(result.Value.EnableStatusTable);
            Assert.IsTrue(result.Value.EnableHeartBeat);
            Assert.IsFalse(result.Value.EnableDelayedProcessing);
            Assert.IsTrue(result.Value.EnableMessageExpiration);
            Assert.IsFalse(result.Value.EnableRoute);
        }

        // === GetQueueConfigurationAsync ===

        [TestMethod]
        public async Task GetQueueConfigurationAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new { ConfigurationJson = "{\"heartbeat\":30}" });
            using var client = CreateClient(json);

            var result = await client.GetQueueConfigurationAsync(Guid.NewGuid());

            Assert.IsTrue(result.Success);
            Assert.AreEqual("{\"heartbeat\":30}", result.Value.ConfigurationJson);
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

            Assert.IsTrue(result.Success);
            Assert.HasCount(1, result.Value.Items);
            Assert.AreEqual("msg-1", result.Value.Items[0].QueueId);
            Assert.AreEqual(50, result.Value.TotalCount);
            Assert.AreEqual(0, result.Value.PageIndex);
            Assert.AreEqual(25, result.Value.PageSize);
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

            StringAssert.Contains(capturedUrl, "status=2");
        }

        // === GetMessageCountAsync ===

        [TestMethod]
        public async Task GetMessageCountAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(42L);
            using var client = CreateClient(json);

            var result = await client.GetMessageCountAsync(Guid.NewGuid());

            Assert.IsTrue(result.Success);
            Assert.AreEqual(42, result.Value);
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

            StringAssert.Contains(capturedUrl, "status=1");
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

            Assert.IsTrue(result.Success);
            Assert.AreEqual("msg-5", result.Value.QueueId);
            Assert.AreEqual(1, result.Value.Status);
            Assert.AreEqual("corr-5", result.Value.CorrelationId);
            Assert.AreEqual("route-b", result.Value.Route);
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

            Assert.IsTrue(result.Success);
            Assert.AreEqual("{\"data\":1}", result.Value.Body);
            Assert.AreEqual("MyMessage", result.Value.TypeName);
            Assert.IsTrue(result.Value.IsEditable);
            Assert.IsFalse(result.Value.IsProcessing);
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

            Assert.IsTrue(result.Success);
            Assert.HasCount(2, result.Value.Headers);
            Assert.AreEqual("val1", result.Value.Headers["key1"]);
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

            Assert.IsTrue(result.Success);
            Assert.HasCount(1, result.Value);
            Assert.AreEqual(1, result.Value[0].ErrorTrackingId);
            Assert.AreEqual("System.Exception", result.Value[0].ExceptionType);
            Assert.AreEqual(3, result.Value[0].RetryCount);
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

            Assert.IsTrue(result.Success);
            Assert.HasCount(1, result.Value.Items);
            Assert.AreEqual("stale-1", result.Value.Items[0].QueueId);
            Assert.AreEqual(5, result.Value.TotalCount);
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

            Assert.IsTrue(result.Success);
            Assert.HasCount(1, result.Value.Items);
            Assert.AreEqual("msg-1", result.Value.Items[0].QueueId);
            Assert.AreEqual("boom", result.Value.Items[0].LastException);
            Assert.AreEqual(10, result.Value.TotalCount);
        }

        // === RequeueErrorMessageAsync ===

        [TestMethod]
        public async Task RequeueErrorMessageAsync_Returns_Success()
        {
            using var client = CreateClient("", HttpStatusCode.NoContent);

            var result = await client.RequeueErrorMessageAsync(Guid.NewGuid(), "msg-1");

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.Value);
        }

        // === ResetMessageAsync (stale) ===

        [TestMethod]
        public async Task ResetStaleMessageAsync_Returns_Success()
        {
            using var client = CreateClient("", HttpStatusCode.NoContent);

            var result = await client.ResetMessageAsync(Guid.NewGuid(), "msg-1");

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.Value);
        }

        // === EditMessageBodyAsync ===

        [TestMethod]
        public async Task EditMessageBodyAsync_Returns_Success()
        {
            using var client = CreateClient("", HttpStatusCode.NoContent);

            var result = await client.EditMessageBodyAsync(Guid.NewGuid(), "msg-1", "{\"updated\":true}");

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.Value);
        }

        // === RequeueAllErrorsAsync ===

        [TestMethod]
        public async Task RequeueAllErrorsAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new { Count = 7L });
            using var client = CreateClient(json);

            var result = await client.RequeueAllErrorsAsync(Guid.NewGuid());

            Assert.IsTrue(result.Success);
            Assert.AreEqual(7, result.Value.Count);
        }

        // === ResetAllStaleMessagesAsync ===

        [TestMethod]
        public async Task ResetAllStaleMessagesAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new { Count = 3L });
            using var client = CreateClient(json);

            var result = await client.ResetAllStaleMessagesAsync(Guid.NewGuid());

            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Value.Count);
        }

        // === DeleteAllErrorsAsync ===

        [TestMethod]
        public async Task DeleteAllErrorsAsync_Returns_Success()
        {
            var json = JsonSerializer.Serialize(new { Deleted = 12L });
            using var client = CreateClient(json);

            var result = await client.DeleteAllErrorsAsync(Guid.NewGuid());

            Assert.IsTrue(result.Success);
            Assert.AreEqual(12, result.Value.Deleted);
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
            Assert.IsNotNull(httpClient.BaseAddress);
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
