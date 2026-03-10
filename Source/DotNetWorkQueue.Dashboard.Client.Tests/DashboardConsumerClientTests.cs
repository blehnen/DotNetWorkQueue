using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Client.Tests
{
    [TestClass]
    public class DashboardConsumerClientTests
    {
        private static DashboardClientOptions CreateOptions(string url = "http://localhost:5000")
        {
            return new DashboardClientOptions
            {
                DashboardApiUrl = url,
                QueueName = "testQueue",
                ConnectionString = "memory",
                FriendlyName = "TestWorker"
            };
        }

        // === Constructor validation ===

        [TestMethod]
        public void Constructor_Throws_When_Options_Null()
        {
            Action act = () => new DashboardConsumerClient(null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor_Throws_When_Url_Missing()
        {
            var opts = CreateOptions();
            opts.DashboardApiUrl = null;
            Action act = () => new DashboardConsumerClient(opts);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Constructor_Throws_When_QueueName_Missing()
        {
            var opts = CreateOptions();
            opts.QueueName = null;
            Action act = () => new DashboardConsumerClient(opts);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Constructor_Throws_When_ConnectionString_Missing()
        {
            var opts = CreateOptions();
            opts.ConnectionString = null;
            Action act = () => new DashboardConsumerClient(opts);
            act.Should().Throw<ArgumentException>();
        }

        // === StartAsync ===

        [TestMethod]
        public async Task StartAsync_Registers_And_Sets_ConsumerId()
        {
            var consumerId = Guid.NewGuid();
            var handler = new MockHandler(req =>
            {
                if (req.RequestUri.PathAndQuery.Contains("/register"))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        ConsumerId = consumerId,
                        HeartbeatIntervalSeconds = 30
                    });
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            var opts = CreateOptions();
            using var client = new DashboardConsumerClient(httpClient, opts);

            await client.StartAsync();

            client.ConsumerId.Should().Be(consumerId);
            client.IsRegistered.Should().BeTrue();
        }

        [TestMethod]
        public async Task StartAsync_Idempotent_When_Already_Registered()
        {
            var callCount = 0;
            var handler = new MockHandler(_ =>
            {
                Interlocked.Increment(ref callCount);
                var json = JsonSerializer.Serialize(new
                {
                    ConsumerId = Guid.NewGuid(),
                    HeartbeatIntervalSeconds = 300
                });
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());

            await client.StartAsync();
            await client.StartAsync();

            callCount.Should().Be(1);
        }

        // === StopAsync ===

        [TestMethod]
        public async Task StopAsync_Clears_ConsumerId()
        {
            var handler = new MockHandler(req =>
            {
                if (req.RequestUri.PathAndQuery.Contains("/register"))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        ConsumerId = Guid.NewGuid(),
                        HeartbeatIntervalSeconds = 300
                    });
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());

            await client.StartAsync();
            client.IsRegistered.Should().BeTrue();

            await client.StopAsync();
            client.IsRegistered.Should().BeFalse();
            client.ConsumerId.Should().BeNull();
        }

        // === Dispose ===

        [TestMethod]
        public void IsRegistered_False_Before_Start()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());

            client.IsRegistered.Should().BeFalse();
            client.ConsumerId.Should().BeNull();
        }

        [TestMethod]
        public void Dispose_Is_Idempotent()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            var client = new DashboardConsumerClient(httpClient, CreateOptions());

            // Should not throw
            client.Dispose();
            client.Dispose();
        }

        // === MockHandler ===

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
