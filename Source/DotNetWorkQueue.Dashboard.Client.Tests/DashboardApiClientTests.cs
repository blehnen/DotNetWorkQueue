using System;
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
