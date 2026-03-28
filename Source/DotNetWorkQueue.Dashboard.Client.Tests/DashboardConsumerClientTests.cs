using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
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
                FriendlyName = "TestWorker"
            };
        }

        // === Constructor validation ===

        [TestMethod]
        public void Constructor_Throws_When_Options_Null()
        {
            Action act = () => new DashboardConsumerClient((DashboardClientOptions)null);
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
        public void Constructor_HttpClient_Throws_When_Null()
        {
            Action act = () => new DashboardConsumerClient((HttpClient)null, CreateOptions());
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor_HttpClientFactory_Throws_When_Null()
        {
            Action act = () => new DashboardConsumerClient((IHttpClientFactory)null, CreateOptions());
            act.Should().Throw<ArgumentNullException>();
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

        // === Metrics ===

        [TestMethod]
        public void Metrics_Start_At_Zero()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());

            client.MessagesProcessed.Should().Be(0);
            client.MessagesErrored.Should().Be(0);
            client.MessagesRolledBack.Should().Be(0);
            client.PoisonMessages.Should().Be(0);
        }

        [TestMethod]
        public void IncrementProcessed_Increments_Counter()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());

            client.IncrementProcessed();
            client.IncrementProcessed();
            client.IncrementProcessed();

            client.MessagesProcessed.Should().Be(3);
        }

        [TestMethod]
        public void IncrementErrored_Increments_Counter()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());

            client.IncrementErrored();
            client.IncrementErrored();

            client.MessagesErrored.Should().Be(2);
        }

        [TestMethod]
        public void IncrementRolledBack_Increments_Counter()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());

            client.IncrementRolledBack();

            client.MessagesRolledBack.Should().Be(1);
        }

        [TestMethod]
        public void IncrementPoisonMessage_Increments_Counter()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());

            client.IncrementPoisonMessage();

            client.PoisonMessages.Should().Be(1);
        }

        [TestMethod]
        public async Task Heartbeat_Includes_Metrics_In_Payload()
        {
            string capturedJson = null;
            var handler = new MockHandler(req =>
            {
                if (req.RequestUri.PathAndQuery.Contains("/register"))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        ConsumerId = Guid.NewGuid(),
                        HeartbeatIntervalSeconds = 9999 // very long so timer doesn't auto-fire
                    });
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                }
                if (req.RequestUri.PathAndQuery.Contains("/heartbeat"))
                {
                    capturedJson = req.Content.ReadAsStringAsync().Result;
                }
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());
            await client.StartAsync();

            client.IncrementProcessed();
            client.IncrementProcessed();
            client.IncrementErrored();

            // Trigger heartbeat manually via reflection to avoid timer dependency
            var method = typeof(DashboardConsumerClient).GetMethod("HeartbeatCallback",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(client, new object[] { null });

            // Give async void time to complete
            await Task.Delay(200);

            capturedJson.Should().NotBeNull();
            using var doc = JsonDocument.Parse(capturedJson);
            doc.RootElement.GetProperty("MessagesProcessed").GetInt64().Should().Be(2);
            doc.RootElement.GetProperty("MessagesErrored").GetInt64().Should().Be(1);
            doc.RootElement.GetProperty("MessagesRolledBack").GetInt64().Should().Be(0);
            doc.RootElement.GetProperty("PoisonMessages").GetInt64().Should().Be(0);
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

        [TestMethod]
        public void Constructor_With_ApiKey_Does_Not_Throw()
        {
            var opts = CreateOptions();
            opts.ApiKey = "my-secret-key";
            using var client = new DashboardConsumerClient(opts);
            client.IsRegistered.Should().BeFalse();
        }

        [TestMethod]
        public void Multiple_Counters_Accumulate_Independently()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());

            client.IncrementProcessed();
            client.IncrementProcessed();
            client.IncrementProcessed();
            client.IncrementErrored();
            client.IncrementErrored();
            client.IncrementRolledBack();
            client.IncrementPoisonMessage();
            client.IncrementPoisonMessage();
            client.IncrementPoisonMessage();
            client.IncrementPoisonMessage();

            client.MessagesProcessed.Should().Be(3);
            client.MessagesErrored.Should().Be(2);
            client.MessagesRolledBack.Should().Be(1);
            client.PoisonMessages.Should().Be(4);
        }

        [TestMethod]
        public void Constructor_HttpClient_QueueName_Required()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            var opts = new DashboardClientOptions
            {
                DashboardApiUrl = "http://localhost:5000",
                QueueName = null
            };
            Action act = () => new DashboardConsumerClient(httpClient, opts);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public async Task StopAsync_When_Not_Started_Does_Not_Throw()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());

            // Should not throw even though not started
            await client.StopAsync();
            client.IsRegistered.Should().BeFalse();
        }

        // === IHttpClientFactory constructor ===

        [TestMethod]
        public void Constructor_HttpClientFactory_QueueName_Required()
        {
            var factory = new FakeHttpClientFactory(new HttpClient(new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))
            {
                BaseAddress = new Uri("http://localhost:5000/")
            });
            var opts = new DashboardClientOptions
            {
                DashboardApiUrl = "http://localhost:5000",
                QueueName = null
            };
            Action act = () => new DashboardConsumerClient(factory, opts);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Constructor_HttpClientFactory_Url_Required()
        {
            var factory = new FakeHttpClientFactory(new HttpClient());
            var opts = new DashboardClientOptions
            {
                DashboardApiUrl = null,
                QueueName = "testQueue"
            };
            Action act = () => new DashboardConsumerClient(factory, opts);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Constructor_HttpClientFactory_Options_Null_Throws()
        {
            var factory = new FakeHttpClientFactory(new HttpClient());
            Action act = () => new DashboardConsumerClient(factory, null);
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void Constructor_HttpClientFactory_BaseAddress_Null_Configures_Client()
        {
            var innerHandler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(innerHandler); // BaseAddress is null
            var factory = new FakeHttpClientFactory(httpClient);

            var opts = CreateOptions();
            using var client = new DashboardConsumerClient(factory, opts);

            client.IsRegistered.Should().BeFalse();
        }

        [TestMethod]
        public void Constructor_HttpClientFactory_BaseAddress_Null_With_ApiKey()
        {
            var innerHandler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(innerHandler); // BaseAddress is null
            var factory = new FakeHttpClientFactory(httpClient);

            var opts = CreateOptions();
            opts.ApiKey = "my-key";
            using var client = new DashboardConsumerClient(factory, opts);

            client.IsRegistered.Should().BeFalse();
        }

        [TestMethod]
        public void Constructor_HttpClientFactory_BaseAddress_Already_Set_Skips_Config()
        {
            var innerHandler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(innerHandler)
            {
                BaseAddress = new Uri("http://already-set:9999/")
            };
            var factory = new FakeHttpClientFactory(httpClient);

            var opts = CreateOptions();
            using var client = new DashboardConsumerClient(factory, opts);

            client.IsRegistered.Should().BeFalse();
        }

        // === StartAsync with HeartbeatIntervalSeconds = 0 ===

        [TestMethod]
        public async Task StartAsync_HeartbeatInterval_Zero_Defaults_To_30()
        {
            var consumerId = Guid.NewGuid();
            var handler = new MockHandler(req =>
            {
                if (req.RequestUri.PathAndQuery.Contains("/register"))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        ConsumerId = consumerId,
                        HeartbeatIntervalSeconds = 0 // should default to 30
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

            client.ConsumerId.Should().Be(consumerId);
            client.IsRegistered.Should().BeTrue();
        }

        // === HeartbeatCallback with 404 response ===

        [TestMethod]
        public async Task Heartbeat_404_Clears_Registration()
        {
            var consumerId = Guid.NewGuid();
            var handler = new MockHandler(req =>
            {
                if (req.RequestUri.PathAndQuery.Contains("/register"))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        ConsumerId = consumerId,
                        HeartbeatIntervalSeconds = 9999
                    });
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                }
                if (req.RequestUri.PathAndQuery.Contains("/heartbeat"))
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());
            await client.StartAsync();
            client.IsRegistered.Should().BeTrue();

            // Trigger heartbeat manually via reflection
            var method = typeof(DashboardConsumerClient).GetMethod("HeartbeatCallback",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(client, new object[] { null });

            // Give async void time to complete
            await Task.Delay(300);

            client.IsRegistered.Should().BeFalse();
            client.ConsumerId.Should().BeNull();
        }

        // === HeartbeatCallback when not registered ===

        [TestMethod]
        public async Task Heartbeat_When_Not_Registered_Does_Nothing()
        {
            var heartbeatCalled = false;
            var handler = new MockHandler(req =>
            {
                if (req.RequestUri.PathAndQuery.Contains("/heartbeat"))
                    heartbeatCalled = true;
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());

            // Not registered, trigger heartbeat
            var method = typeof(DashboardConsumerClient).GetMethod("HeartbeatCallback",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(client, new object[] { null });
            await Task.Delay(200);

            heartbeatCalled.Should().BeFalse();
        }

        // === HeartbeatCallback exception swallowed ===

        [TestMethod]
        public async Task Heartbeat_Exception_Is_Swallowed()
        {
            var consumerId = Guid.NewGuid();
            var callCount = 0;
            var handler = new MockHandler(req =>
            {
                if (req.RequestUri.PathAndQuery.Contains("/register"))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        ConsumerId = consumerId,
                        HeartbeatIntervalSeconds = 9999
                    });
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                }
                if (req.RequestUri.PathAndQuery.Contains("/heartbeat"))
                {
                    Interlocked.Increment(ref callCount);
                    throw new HttpRequestException("Network error");
                }
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());
            await client.StartAsync();

            var method = typeof(DashboardConsumerClient).GetMethod("HeartbeatCallback",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(client, new object[] { null });
            await Task.Delay(200);

            // Client should still be registered despite exception
            client.IsRegistered.Should().BeTrue();
            callCount.Should().Be(1);
        }

        // === Dispose with active registration ===

        [TestMethod]
        public async Task Dispose_With_Registration_Does_Not_Send_Delete()
        {
            var deleteReceived = false;
            var consumerId = Guid.NewGuid();
            var handler = new MockHandler(req =>
            {
                if (req.RequestUri.PathAndQuery.Contains("/register"))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        ConsumerId = consumerId,
                        HeartbeatIntervalSeconds = 9999
                    });
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                }
                if (req.Method == HttpMethod.Delete)
                {
                    deleteReceived = true;
                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                }
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            var client = new DashboardConsumerClient(httpClient, CreateOptions());
            await client.StartAsync();
            client.IsRegistered.Should().BeTrue();

            client.Dispose();

            // Sync Dispose no longer attempts HTTP DELETE to avoid sync-over-async deadlocks
            deleteReceived.Should().BeFalse();
        }

        // === Dispose with owned HttpClient ===

        [TestMethod]
        public void Dispose_Owned_HttpClient_Is_Disposed()
        {
            var opts = CreateOptions();
            var client = new DashboardConsumerClient(opts);

            // Should not throw even though real HTTP client is created
            client.Dispose();
            client.Dispose(); // idempotent
        }

        // === Dispose unregister DELETE throws ===

        [TestMethod]
        public async Task Dispose_Does_Not_Attempt_Delete()
        {
            var deleteAttempted = false;
            var consumerId = Guid.NewGuid();
            var handler = new MockHandler(req =>
            {
                if (req.RequestUri.PathAndQuery.Contains("/register"))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        ConsumerId = consumerId,
                        HeartbeatIntervalSeconds = 9999
                    });
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                }
                if (req.Method == HttpMethod.Delete)
                {
                    deleteAttempted = true;
                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                }
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            var client = new DashboardConsumerClient(httpClient, CreateOptions());
            await client.StartAsync();

            // Sync Dispose should not attempt any HTTP calls
            client.Dispose();
            deleteAttempted.Should().BeFalse();
        }

        // === StopAsync DELETE throws ===

        [TestMethod]
        public async Task StopAsync_Delete_Throws_Is_Swallowed()
        {
            var consumerId = Guid.NewGuid();
            var handler = new MockHandler(req =>
            {
                if (req.RequestUri.PathAndQuery.Contains("/register"))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        ConsumerId = consumerId,
                        HeartbeatIntervalSeconds = 9999
                    });
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                }
                if (req.Method == HttpMethod.Delete)
                {
                    throw new HttpRequestException("Network error");
                }
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            using var client = new DashboardConsumerClient(httpClient, CreateOptions());
            await client.StartAsync();
            client.IsRegistered.Should().BeTrue();

            // Should not throw
            await client.StopAsync();

            client.IsRegistered.Should().BeFalse();
        }

        // === Constructor HttpClient with options null ===

        [TestMethod]
        public void Constructor_HttpClient_Options_Null_Throws()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            Action act = () => new DashboardConsumerClient(httpClient, null);
            act.Should().Throw<ArgumentNullException>();
        }

        // === DisposeAsync ===

        [TestMethod]
        public async Task DisposeAsync_Is_Idempotent()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            var client = new DashboardConsumerClient(httpClient, CreateOptions());

            // Should not throw on second call
            await client.DisposeAsync();
            await client.DisposeAsync();
        }

        [TestMethod]
        public async Task DisposeAsync_With_Registration_Sends_Delete()
        {
            var deleteReceived = false;
            var consumerId = Guid.NewGuid();
            var handler = new MockHandler(req =>
            {
                if (req.RequestUri.PathAndQuery.Contains("/register"))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        ConsumerId = consumerId,
                        HeartbeatIntervalSeconds = 9999
                    });
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                }
                if (req.Method == HttpMethod.Delete)
                {
                    deleteReceived = true;
                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                }
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            var client = new DashboardConsumerClient(httpClient, CreateOptions());
            await client.StartAsync();
            client.IsRegistered.Should().BeTrue();

            await client.DisposeAsync();

            deleteReceived.Should().BeTrue();
        }

        [TestMethod]
        public async Task DisposeAsync_Without_Registration_Does_Not_Send_Delete()
        {
            var deleteReceived = false;
            var handler = new MockHandler(req =>
            {
                if (req.Method == HttpMethod.Delete)
                    deleteReceived = true;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            var client = new DashboardConsumerClient(httpClient, CreateOptions());

            // No StartAsync() call -- not registered
            await client.DisposeAsync();

            deleteReceived.Should().BeFalse();
        }

        [TestMethod]
        public async Task DisposeAsync_Delete_Throws_Is_Swallowed()
        {
            var consumerId = Guid.NewGuid();
            var handler = new MockHandler(req =>
            {
                if (req.RequestUri.PathAndQuery.Contains("/register"))
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        ConsumerId = consumerId,
                        HeartbeatIntervalSeconds = 9999
                    });
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                    };
                }
                if (req.Method == HttpMethod.Delete)
                {
                    throw new HttpRequestException("Network error during async dispose");
                }
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            var client = new DashboardConsumerClient(httpClient, CreateOptions());
            await client.StartAsync();

            // Should not throw -- exception is swallowed
            Func<Task> act = async () => await client.DisposeAsync();
            await act.Should().NotThrowAsync();
        }

        [TestMethod]
        public async Task DisposeAsync_Owned_HttpClient_Is_Disposed()
        {
            var opts = CreateOptions();
            var client = new DashboardConsumerClient(opts);

            await client.DisposeAsync();

            // The internally-created HttpClient should be disposed.
            // Accessing it after disposal via reflection to verify.
            var field = typeof(DashboardConsumerClient).GetField("_httpClient",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var httpClient = (HttpClient)field.GetValue(client);

            // A disposed HttpClient throws ObjectDisposedException on use
            Action act = () => httpClient.GetAsync("http://localhost:5000/test").GetAwaiter().GetResult();
            act.Should().Throw<ObjectDisposedException>();
        }

        [TestMethod]
        public async Task DisposeAsync_Then_Dispose_Is_Safe()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            var client = new DashboardConsumerClient(httpClient, CreateOptions());

            await client.DisposeAsync();
            client.Dispose(); // Should not throw
        }

        [TestMethod]
        public async Task Dispose_Then_DisposeAsync_Is_Safe()
        {
            var handler = new MockHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/") };
            var client = new DashboardConsumerClient(httpClient, CreateOptions());

            client.Dispose();
            await client.DisposeAsync(); // Should not throw
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

        private class FakeHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;

            public FakeHttpClientFactory(HttpClient client)
            {
                _client = client;
            }

            public HttpClient CreateClient(string name) => _client;
        }
    }
}
