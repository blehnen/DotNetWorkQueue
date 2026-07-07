using System;
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Dashboard.Api.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Services
{
    [TestClass]
    public class ConsumerRegistryTests
    {
        private static IConsumerRegistry CreateRegistry(
            out Guid queueId,
            string queueName = "testQueue")
        {
            queueId = Guid.NewGuid();
            var connectionId = Guid.NewGuid();

            var queueInfo = new DashboardQueueInfo();
            typeof(DashboardQueueInfo).GetProperty("Id")!.SetValue(queueInfo, queueId);
            typeof(DashboardQueueInfo).GetProperty("QueueName")!.SetValue(queueInfo, queueName);
            typeof(DashboardQueueInfo).GetProperty("ConnectionString")!.SetValue(queueInfo, "memory");

            var connectionInfo = new DashboardConnectionInfo();
            typeof(DashboardConnectionInfo).GetProperty("Id")!.SetValue(connectionInfo, connectionId);
            typeof(DashboardConnectionInfo).GetProperty("ConnectionString")!.SetValue(connectionInfo, "memory");
            typeof(DashboardConnectionInfo).GetProperty("Queues")!.SetValue(connectionInfo,
                (IReadOnlyList<DashboardQueueInfo>)new List<DashboardQueueInfo> { queueInfo });

            var api = Substitute.For<IDashboardApi>();
            api.Connections.Returns(new Dictionary<Guid, DashboardConnectionInfo>
            {
                { connectionId, connectionInfo }
            });

            return new ConsumerRegistry(api);
        }

        [TestMethod]
        public void Register_Returns_NonEmpty_Guid()
        {
            var registry = CreateRegistry(out _);
            var id = registry.Register("testQueue", "MACHINE1", 1234, null);
            Assert.AreNotEqual(Guid.Empty, id);
        }

        [TestMethod]
        public void Register_Multiple_Returns_Unique_Ids()
        {
            var registry = CreateRegistry(out _);
            var id1 = registry.Register("testQueue", "MACHINE1", 1234, null);
            var id2 = registry.Register("testQueue", "MACHINE1", 5678, null);
            Assert.AreNotEqual(id2, id1);
        }

        [TestMethod]
        public void Register_Matches_Queue_By_Name()
        {
            var registry = CreateRegistry(out var queueId);
            registry.Register("testQueue", "MACHINE1", 1234, null);

            var entries = registry.GetAll();
            Assert.HasCount(1, entries);
            Assert.AreEqual(queueId, entries[0].MatchedQueueId);
        }

        [TestMethod]
        public void Register_No_Match_When_QueueName_Differs()
        {
            var registry = CreateRegistry(out _);
            registry.Register("otherQueue", "MACHINE1", 1234, null);

            var entries = registry.GetAll();
            Assert.IsNull(entries[0].MatchedQueueId);
        }

        [TestMethod]
        public void Register_Sets_FriendlyName()
        {
            var registry = CreateRegistry(out _);
            registry.Register("testQueue", "MACHINE1", 1234, "MyConsumer");

            var entries = registry.GetAll();
            Assert.AreEqual("MyConsumer", entries[0].FriendlyName);
        }

        [TestMethod]
        public void Register_Sets_RegisteredAt_And_LastHeartbeat()
        {
            var registry = CreateRegistry(out _);
            var before = DateTimeOffset.UtcNow;
            registry.Register("testQueue", "MACHINE1", 1234, null);
            var after = DateTimeOffset.UtcNow;

            var entry = registry.GetAll()[0];
            Assert.IsGreaterThanOrEqualTo(before, entry.RegisteredAt);
            Assert.IsLessThanOrEqualTo(after, entry.RegisteredAt);
            Assert.IsGreaterThanOrEqualTo(before, entry.LastHeartbeat);
            Assert.IsLessThanOrEqualTo(after, entry.LastHeartbeat);
        }

        [TestMethod]
        public void Heartbeat_Updates_LastHeartbeat()
        {
            var registry = CreateRegistry(out _);
            var id = registry.Register("testQueue", "MACHINE1", 1234, null);
            var initialHeartbeat = registry.GetAll()[0].LastHeartbeat;

            System.Threading.Thread.Sleep(10);
            var result = registry.Heartbeat(id);

            Assert.IsTrue(result);
            Assert.IsGreaterThan(initialHeartbeat, registry.GetAll()[0].LastHeartbeat);
        }

        [TestMethod]
        public void Heartbeat_Returns_False_For_Unknown_Id()
        {
            var registry = CreateRegistry(out _);
            Assert.IsFalse(registry.Heartbeat(Guid.NewGuid()));
        }

        [TestMethod]
        public void Heartbeat_Updates_Metrics()
        {
            var registry = CreateRegistry(out _);
            var id = registry.Register("testQueue", "MACHINE1", 1234, null);

            registry.Heartbeat(id, messagesProcessed: 100, messagesErrored: 5, messagesRolledBack: 3, poisonMessages: 1);

            var entry = registry.GetAll()[0];
            Assert.AreEqual(100, entry.MessagesProcessed);
            Assert.AreEqual(5, entry.MessagesErrored);
            Assert.AreEqual(3, entry.MessagesRolledBack);
            Assert.AreEqual(1, entry.PoisonMessages);
        }

        [TestMethod]
        public void Heartbeat_Overwrites_Previous_Metrics()
        {
            var registry = CreateRegistry(out _);
            var id = registry.Register("testQueue", "MACHINE1", 1234, null);

            registry.Heartbeat(id, messagesProcessed: 50, messagesErrored: 2, messagesRolledBack: 1, poisonMessages: 0);
            registry.Heartbeat(id, messagesProcessed: 200, messagesErrored: 8, messagesRolledBack: 4, poisonMessages: 1);

            var entry = registry.GetAll()[0];
            Assert.AreEqual(200, entry.MessagesProcessed);
            Assert.AreEqual(8, entry.MessagesErrored);
            Assert.AreEqual(4, entry.MessagesRolledBack);
            Assert.AreEqual(1, entry.PoisonMessages);
        }

        [TestMethod]
        public void Register_Initializes_Metrics_To_Zero()
        {
            var registry = CreateRegistry(out _);
            registry.Register("testQueue", "MACHINE1", 1234, null);

            var entry = registry.GetAll()[0];
            Assert.AreEqual(0, entry.MessagesProcessed);
            Assert.AreEqual(0, entry.MessagesErrored);
            Assert.AreEqual(0, entry.MessagesRolledBack);
            Assert.AreEqual(0, entry.PoisonMessages);
        }

        [TestMethod]
        public void Unregister_Removes_Consumer()
        {
            var registry = CreateRegistry(out _);
            var id = registry.Register("testQueue", "MACHINE1", 1234, null);

            Assert.IsTrue(registry.Unregister(id));
            Assert.IsEmpty(registry.GetAll());
        }

        [TestMethod]
        public void Unregister_Returns_False_For_Unknown_Id()
        {
            var registry = CreateRegistry(out _);
            Assert.IsFalse(registry.Unregister(Guid.NewGuid()));
        }

        [TestMethod]
        public void GetAll_Returns_All_Registered()
        {
            var registry = CreateRegistry(out _);
            registry.Register("testQueue", "MACHINE1", 1234, null);
            registry.Register("testQueue", "MACHINE2", 5678, null);

            Assert.HasCount(2, registry.GetAll());
        }

        [TestMethod]
        public void GetByQueue_Returns_Only_Matched()
        {
            var registry = CreateRegistry(out var queueId);
            registry.Register("testQueue", "MACHINE1", 1234, null);
            registry.Register("otherQueue", "MACHINE2", 5678, null);

            var matched = registry.GetByQueue(queueId);
            Assert.HasCount(1, matched);
            Assert.AreEqual("MACHINE1", matched[0].MachineName);
        }

        [TestMethod]
        public void GetByQueue_Returns_Empty_When_No_Match()
        {
            var registry = CreateRegistry(out _);
            registry.Register("testQueue", "MACHINE1", 1234, null);

            Assert.IsEmpty(registry.GetByQueue(Guid.NewGuid()));
        }

        [TestMethod]
        public void GetCountsByQueue_Returns_Counts()
        {
            var registry = CreateRegistry(out var queueId);
            registry.Register("testQueue", "MACHINE1", 1234, null);
            registry.Register("testQueue", "MACHINE2", 5678, null);
            registry.Register("otherQueue", "MACHINE3", 9012, null);

            var counts = registry.GetCountsByQueue();
            Assert.IsTrue((counts).ContainsKey(queueId));
            Assert.AreEqual(2, counts[queueId]);
        }

        [TestMethod]
        public void GetCountsByQueue_Excludes_Unmatched()
        {
            var registry = CreateRegistry(out _);
            registry.Register("unknownQueue", "MACHINE1", 1234, null);

            Assert.IsEmpty(registry.GetCountsByQueue());
        }

        [TestMethod]
        public void PruneStale_Removes_Expired_Consumers()
        {
            var registry = CreateRegistry(out _);
            registry.Register("testQueue", "MACHINE1", 1234, null);

            // With a zero threshold, everything is stale
            var pruned = registry.PruneStale(TimeSpan.Zero);
            Assert.AreEqual(1, pruned);
            Assert.IsEmpty(registry.GetAll());
        }

        [TestMethod]
        public void PruneStale_Keeps_Fresh_Consumers()
        {
            var registry = CreateRegistry(out _);
            registry.Register("testQueue", "MACHINE1", 1234, null);

            var pruned = registry.PruneStale(TimeSpan.FromMinutes(5));
            Assert.AreEqual(0, pruned);
            Assert.HasCount(1, registry.GetAll());
        }

        [TestMethod]
        public void PruneStale_Returns_Zero_When_Empty()
        {
            var registry = CreateRegistry(out _);
            Assert.AreEqual(0, registry.PruneStale(TimeSpan.Zero));
        }
    }
}
