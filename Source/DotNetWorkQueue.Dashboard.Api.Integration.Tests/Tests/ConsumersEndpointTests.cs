using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using DotNetWorkQueue.Dashboard.Api.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class ConsumersEndpointTests
    {
        private DashboardTestServer _server;
        private Guid _queueId;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<Transport.Memory.Basic.MemoryDashboardInit>(
                    "memory-consumers-test",
                    conn => conn.AddQueue("testQueue"));
            });

            var connections = await _server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await _server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            _queueId = queues[0].Id;
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_server != null) await _server.DisposeAsync();
        }

        // === Registration ===

        [TestMethod]
        public async Task Register_Returns_201_With_ConsumerId()
        {
            var response = await RegisterConsumer("MACHINE1", 1234, "Worker1");

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var registration = await response.Content.ReadFromJsonAsync<ConsumerRegistrationResponse>();
            registration.Should().NotBeNull();
            registration!.ConsumerId.Should().NotBeEmpty();
            registration.HeartbeatIntervalSeconds.Should().BeGreaterThan(0);
        }

        [TestMethod]
        public async Task Register_Multiple_Get_Unique_Ids()
        {
            var r1 = await RegisterAndDeserialize("M1", 1000);
            var r2 = await RegisterAndDeserialize("M2", 2000);

            r1.ConsumerId.Should().NotBe(r2.ConsumerId);
        }

        // === Heartbeat ===

        [TestMethod]
        public async Task Heartbeat_Returns_NoContent()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);

            var response = await SendHeartbeat(registration.ConsumerId);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [TestMethod]
        public async Task Heartbeat_Returns_NotFound_For_Unknown()
        {
            var response = await SendHeartbeat(Guid.NewGuid());

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Heartbeat_With_Metrics_Returns_NoContent()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);

            var response = await SendHeartbeat(registration.ConsumerId, 100, 5, 3, 1);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [TestMethod]
        public async Task Heartbeat_Metrics_Appear_In_Consumer_List()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);
            await SendHeartbeat(registration.ConsumerId, 250, 10, 7, 2);

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");

            consumers.Should().HaveCount(1);
            var c = consumers![0];
            c.MessagesProcessed.Should().Be(250);
            c.MessagesErrored.Should().Be(10);
            c.MessagesRolledBack.Should().Be(7);
            c.PoisonMessages.Should().Be(2);
        }

        [TestMethod]
        public async Task Heartbeat_Metrics_Update_On_Subsequent_Heartbeats()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);
            await SendHeartbeat(registration.ConsumerId, 100, 2, 1, 0);
            await SendHeartbeat(registration.ConsumerId, 500, 8, 4, 1);

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");

            var c = consumers![0];
            c.MessagesProcessed.Should().Be(500);
            c.MessagesErrored.Should().Be(8);
            c.MessagesRolledBack.Should().Be(4);
            c.PoisonMessages.Should().Be(1);
        }

        [TestMethod]
        public async Task Heartbeat_Without_Metrics_Defaults_To_Zero()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);
            await SendHeartbeat(registration.ConsumerId);

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");

            var c = consumers![0];
            c.MessagesProcessed.Should().Be(0);
            c.MessagesErrored.Should().Be(0);
            c.MessagesRolledBack.Should().Be(0);
            c.PoisonMessages.Should().Be(0);
        }

        // === Unregister ===

        [TestMethod]
        public async Task Unregister_Returns_NoContent()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);

            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/consumers/{registration.ConsumerId}");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [TestMethod]
        public async Task Unregister_Returns_NotFound_For_Unknown()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/consumers/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Unregister_Removes_From_List()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);

            await _server.Client.DeleteAsync(
                $"api/v1/dashboard/consumers/{registration.ConsumerId}");

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            consumers.Should().BeEmpty();
        }

        // === List ===

        [TestMethod]
        public async Task GetConsumers_Returns_All()
        {
            await RegisterConsumer("M1", 1000);
            await RegisterConsumer("M2", 2000);

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");

            consumers.Should().HaveCount(2);
        }

        [TestMethod]
        public async Task GetConsumers_Filters_By_QueueId()
        {
            await RegisterConsumer("M1", 1000);

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                $"api/v1/dashboard/consumers?queueId={_queueId}");

            consumers.Should().HaveCount(1);
            consumers![0].MatchedQueueId.Should().Be(_queueId);
        }

        [TestMethod]
        public async Task GetConsumers_Empty_For_Unknown_Queue()
        {
            await RegisterConsumer("M1", 1000);

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                $"api/v1/dashboard/consumers?queueId={Guid.NewGuid()}");

            consumers.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetConsumers_Contains_Expected_Fields()
        {
            await RegisterConsumer("TESTMACHINE", 9876, "MyWorker");

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");

            consumers.Should().HaveCount(1);
            var c = consumers![0];
            c.MachineName.Should().Be("TESTMACHINE");
            c.ProcessId.Should().Be(9876);
            c.FriendlyName.Should().Be("MyWorker");
            c.QueueName.Should().Be("testQueue");
            c.RegisteredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
            c.LastHeartbeat.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        }

        // === Counts ===

        [TestMethod]
        public async Task GetConsumerCounts_Returns_Counts()
        {
            await RegisterConsumer("M1", 1000);
            await RegisterConsumer("M2", 2000);

            var counts = await _server.Client.GetFromJsonAsync<Dictionary<Guid, int>>(
                "api/v1/dashboard/consumers/count");

            counts.Should().ContainKey(_queueId);
            counts![_queueId].Should().Be(2);
        }

        [TestMethod]
        public async Task GetConsumerCounts_Empty_When_None()
        {
            var counts = await _server.Client.GetFromJsonAsync<Dictionary<Guid, int>>(
                "api/v1/dashboard/consumers/count");

            counts.Should().BeEmpty();
        }

        // === Full lifecycle ===

        [TestMethod]
        public async Task FullLifecycle_Register_Heartbeat_List_Unregister()
        {
            // Register
            var registration = await RegisterAndDeserialize("M1", 1000, "Worker1");
            registration.ConsumerId.Should().NotBeEmpty();

            // Heartbeat with metrics
            var hbResponse = await SendHeartbeat(registration.ConsumerId, 42, 3, 2, 1);
            hbResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // List — verify metrics are present
            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            consumers.Should().HaveCount(1);
            consumers![0].MessagesProcessed.Should().Be(42);
            consumers[0].MessagesErrored.Should().Be(3);
            consumers[0].MessagesRolledBack.Should().Be(2);
            consumers[0].PoisonMessages.Should().Be(1);

            // Count
            var counts = await _server.Client.GetFromJsonAsync<Dictionary<Guid, int>>(
                "api/v1/dashboard/consumers/count");
            counts![_queueId].Should().Be(1);

            // Unregister
            var deleteResponse = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/consumers/{registration.ConsumerId}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify gone
            consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            consumers.Should().BeEmpty();
        }

        // === Helpers ===

        private async Task<HttpResponseMessage> RegisterConsumer(string machine, int pid, string friendlyName = null)
        {
            var body = new
            {
                QueueName = "testQueue",
                MachineName = machine,
                ProcessId = pid,
                FriendlyName = friendlyName
            };
            return await _server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/register", body);
        }

        private async Task<ConsumerRegistrationResponse> RegisterAndDeserialize(string machine, int pid, string friendlyName = null)
        {
            var response = await RegisterConsumer(machine, pid, friendlyName);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ConsumerRegistrationResponse>();
        }

        private async Task<HttpResponseMessage> SendHeartbeat(Guid consumerId, long messagesProcessed = 0, long messagesErrored = 0, long messagesRolledBack = 0, long poisonMessages = 0)
        {
            var body = new
            {
                ConsumerId = consumerId,
                MessagesProcessed = messagesProcessed,
                MessagesErrored = messagesErrored,
                MessagesRolledBack = messagesRolledBack,
                PoisonMessages = poisonMessages
            };
            return await _server.Client.PostAsJsonAsync("api/v1/dashboard/consumers/heartbeat", body);
        }
    }
}
