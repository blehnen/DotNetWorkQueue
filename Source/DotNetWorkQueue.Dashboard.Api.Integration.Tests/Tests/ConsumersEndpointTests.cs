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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.Tests.Shared;

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

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            var registration = await response.Content.ReadFromJsonAsync<ConsumerRegistrationResponse>();
            Assert.IsNotNull(registration);
            Assert.AreNotEqual(Guid.Empty, registration!.ConsumerId);
            Assert.IsTrue(registration.HeartbeatIntervalSeconds > 0);
        }

        [TestMethod]
        public async Task Register_Multiple_Get_Unique_Ids()
        {
            var r1 = await RegisterAndDeserialize("M1", 1000);
            var r2 = await RegisterAndDeserialize("M2", 2000);

            Assert.AreNotEqual(r2.ConsumerId, r1.ConsumerId);
        }

        // === Heartbeat ===

        [TestMethod]
        public async Task Heartbeat_Returns_NoContent()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);

            var response = await SendHeartbeat(registration.ConsumerId);

            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        [TestMethod]
        public async Task Heartbeat_Returns_NotFound_For_Unknown()
        {
            var response = await SendHeartbeat(Guid.NewGuid());

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Heartbeat_With_Metrics_Returns_NoContent()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);

            var response = await SendHeartbeat(registration.ConsumerId, 100, 5, 3, 1);

            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        [TestMethod]
        public async Task Heartbeat_Metrics_Appear_In_Consumer_List()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);
            await SendHeartbeat(registration.ConsumerId, 250, 10, 7, 2);

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");

            Assert.AreEqual(1, consumers.Count);
            var c = consumers![0];
            Assert.AreEqual(250, c.MessagesProcessed);
            Assert.AreEqual(10, c.MessagesErrored);
            Assert.AreEqual(7, c.MessagesRolledBack);
            Assert.AreEqual(2, c.PoisonMessages);
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
            Assert.AreEqual(500, c.MessagesProcessed);
            Assert.AreEqual(8, c.MessagesErrored);
            Assert.AreEqual(4, c.MessagesRolledBack);
            Assert.AreEqual(1, c.PoisonMessages);
        }

        [TestMethod]
        public async Task Heartbeat_Without_Metrics_Defaults_To_Zero()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);
            await SendHeartbeat(registration.ConsumerId);

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");

            var c = consumers![0];
            Assert.AreEqual(0, c.MessagesProcessed);
            Assert.AreEqual(0, c.MessagesErrored);
            Assert.AreEqual(0, c.MessagesRolledBack);
            Assert.AreEqual(0, c.PoisonMessages);
        }

        // === Unregister ===

        [TestMethod]
        public async Task Unregister_Returns_NoContent()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);

            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/consumers/{registration.ConsumerId}");

            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        [TestMethod]
        public async Task Unregister_Returns_NotFound_For_Unknown()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/consumers/{Guid.NewGuid()}");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Unregister_Removes_From_List()
        {
            var registration = await RegisterAndDeserialize("M1", 1000);

            await _server.Client.DeleteAsync(
                $"api/v1/dashboard/consumers/{registration.ConsumerId}");

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            Assert.AreEqual(0, consumers.Count);
        }

        // === List ===

        [TestMethod]
        public async Task GetConsumers_Returns_All()
        {
            await RegisterConsumer("M1", 1000);
            await RegisterConsumer("M2", 2000);

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");

            Assert.AreEqual(2, consumers.Count);
        }

        [TestMethod]
        public async Task GetConsumers_Filters_By_QueueId()
        {
            await RegisterConsumer("M1", 1000);

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                $"api/v1/dashboard/consumers?queueId={_queueId}");

            Assert.AreEqual(1, consumers.Count);
            Assert.AreEqual(_queueId, consumers![0].MatchedQueueId);
        }

        [TestMethod]
        public async Task GetConsumers_Empty_For_Unknown_Queue()
        {
            await RegisterConsumer("M1", 1000);

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                $"api/v1/dashboard/consumers?queueId={Guid.NewGuid()}");

            Assert.AreEqual(0, consumers.Count);
        }

        [TestMethod]
        public async Task GetConsumers_Contains_Expected_Fields()
        {
            await RegisterConsumer("TESTMACHINE", 9876, "MyWorker");

            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");

            Assert.AreEqual(1, consumers.Count);
            var c = consumers![0];
            Assert.AreEqual("TESTMACHINE", c.MachineName);
            Assert.AreEqual(9876, c.ProcessId);
            Assert.AreEqual("MyWorker", c.FriendlyName);
            Assert.AreEqual("testQueue", c.QueueName);
            AssertHelper.AreClose(DateTimeOffset.UtcNow, c.RegisteredAt, TimeSpan.FromSeconds(5));
            AssertHelper.AreClose(DateTimeOffset.UtcNow, c.LastHeartbeat, TimeSpan.FromSeconds(5));
        }

        // === Counts ===

        [TestMethod]
        public async Task GetConsumerCounts_Returns_Counts()
        {
            await RegisterConsumer("M1", 1000);
            await RegisterConsumer("M2", 2000);

            var counts = await _server.Client.GetFromJsonAsync<Dictionary<Guid, int>>(
                "api/v1/dashboard/consumers/count");

            Assert.IsTrue(counts.ContainsKey(_queueId));
            Assert.AreEqual(2, counts![_queueId]);
        }

        [TestMethod]
        public async Task GetConsumerCounts_Empty_When_None()
        {
            var counts = await _server.Client.GetFromJsonAsync<Dictionary<Guid, int>>(
                "api/v1/dashboard/consumers/count");

            Assert.AreEqual(0, counts.Count);
        }

        // === Full lifecycle ===

        [TestMethod]
        public async Task FullLifecycle_Register_Heartbeat_List_Unregister()
        {
            // Register
            var registration = await RegisterAndDeserialize("M1", 1000, "Worker1");
            Assert.AreNotEqual(Guid.Empty, registration.ConsumerId);

            // Heartbeat with metrics
            var hbResponse = await SendHeartbeat(registration.ConsumerId, 42, 3, 2, 1);
            Assert.AreEqual(HttpStatusCode.NoContent, hbResponse.StatusCode);

            // List — verify metrics are present
            var consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            Assert.AreEqual(1, consumers.Count);
            Assert.AreEqual(42, consumers![0].MessagesProcessed);
            Assert.AreEqual(3, consumers[0].MessagesErrored);
            Assert.AreEqual(2, consumers[0].MessagesRolledBack);
            Assert.AreEqual(1, consumers[0].PoisonMessages);

            // Count
            var counts = await _server.Client.GetFromJsonAsync<Dictionary<Guid, int>>(
                "api/v1/dashboard/consumers/count");
            Assert.AreEqual(1, counts![_queueId]);

            // Unregister
            var deleteResponse = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/consumers/{registration.ConsumerId}");
            Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Verify gone
            consumers = await _server.Client.GetFromJsonAsync<List<ConsumerInfoResponse>>(
                "api/v1/dashboard/consumers");
            Assert.AreEqual(0, consumers.Count);
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
