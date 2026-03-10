using System;
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Dashboard.Api.Services;
using FluentAssertions;
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
            id.Should().NotBeEmpty();
        }

        [TestMethod]
        public void Register_Multiple_Returns_Unique_Ids()
        {
            var registry = CreateRegistry(out _);
            var id1 = registry.Register("testQueue", "MACHINE1", 1234, null);
            var id2 = registry.Register("testQueue", "MACHINE1", 5678, null);
            id1.Should().NotBe(id2);
        }

        [TestMethod]
        public void Register_Matches_Queue_By_Name()
        {
            var registry = CreateRegistry(out var queueId);
            registry.Register("testQueue", "MACHINE1", 1234, null);

            var entries = registry.GetAll();
            entries.Should().HaveCount(1);
            entries[0].MatchedQueueId.Should().Be(queueId);
        }

        [TestMethod]
        public void Register_No_Match_When_QueueName_Differs()
        {
            var registry = CreateRegistry(out _);
            registry.Register("otherQueue", "MACHINE1", 1234, null);

            var entries = registry.GetAll();
            entries[0].MatchedQueueId.Should().BeNull();
        }

        [TestMethod]
        public void Register_Sets_FriendlyName()
        {
            var registry = CreateRegistry(out _);
            registry.Register("testQueue", "MACHINE1", 1234, "MyConsumer");

            var entries = registry.GetAll();
            entries[0].FriendlyName.Should().Be("MyConsumer");
        }

        [TestMethod]
        public void Register_Sets_RegisteredAt_And_LastHeartbeat()
        {
            var registry = CreateRegistry(out _);
            var before = DateTimeOffset.UtcNow;
            registry.Register("testQueue", "MACHINE1", 1234, null);
            var after = DateTimeOffset.UtcNow;

            var entry = registry.GetAll()[0];
            entry.RegisteredAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
            entry.LastHeartbeat.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        }

        [TestMethod]
        public void Heartbeat_Updates_LastHeartbeat()
        {
            var registry = CreateRegistry(out _);
            var id = registry.Register("testQueue", "MACHINE1", 1234, null);
            var initialHeartbeat = registry.GetAll()[0].LastHeartbeat;

            System.Threading.Thread.Sleep(10);
            var result = registry.Heartbeat(id);

            result.Should().BeTrue();
            registry.GetAll()[0].LastHeartbeat.Should().BeAfter(initialHeartbeat);
        }

        [TestMethod]
        public void Heartbeat_Returns_False_For_Unknown_Id()
        {
            var registry = CreateRegistry(out _);
            registry.Heartbeat(Guid.NewGuid()).Should().BeFalse();
        }

        [TestMethod]
        public void Unregister_Removes_Consumer()
        {
            var registry = CreateRegistry(out _);
            var id = registry.Register("testQueue", "MACHINE1", 1234, null);

            registry.Unregister(id).Should().BeTrue();
            registry.GetAll().Should().BeEmpty();
        }

        [TestMethod]
        public void Unregister_Returns_False_For_Unknown_Id()
        {
            var registry = CreateRegistry(out _);
            registry.Unregister(Guid.NewGuid()).Should().BeFalse();
        }

        [TestMethod]
        public void GetAll_Returns_All_Registered()
        {
            var registry = CreateRegistry(out _);
            registry.Register("testQueue", "MACHINE1", 1234, null);
            registry.Register("testQueue", "MACHINE2", 5678, null);

            registry.GetAll().Should().HaveCount(2);
        }

        [TestMethod]
        public void GetByQueue_Returns_Only_Matched()
        {
            var registry = CreateRegistry(out var queueId);
            registry.Register("testQueue", "MACHINE1", 1234, null);
            registry.Register("otherQueue", "MACHINE2", 5678, null);

            var matched = registry.GetByQueue(queueId);
            matched.Should().HaveCount(1);
            matched[0].MachineName.Should().Be("MACHINE1");
        }

        [TestMethod]
        public void GetByQueue_Returns_Empty_When_No_Match()
        {
            var registry = CreateRegistry(out _);
            registry.Register("testQueue", "MACHINE1", 1234, null);

            registry.GetByQueue(Guid.NewGuid()).Should().BeEmpty();
        }

        [TestMethod]
        public void GetCountsByQueue_Returns_Counts()
        {
            var registry = CreateRegistry(out var queueId);
            registry.Register("testQueue", "MACHINE1", 1234, null);
            registry.Register("testQueue", "MACHINE2", 5678, null);
            registry.Register("otherQueue", "MACHINE3", 9012, null);

            var counts = registry.GetCountsByQueue();
            counts.Should().ContainKey(queueId);
            counts[queueId].Should().Be(2);
        }

        [TestMethod]
        public void GetCountsByQueue_Excludes_Unmatched()
        {
            var registry = CreateRegistry(out _);
            registry.Register("unknownQueue", "MACHINE1", 1234, null);

            registry.GetCountsByQueue().Should().BeEmpty();
        }

        [TestMethod]
        public void PruneStale_Removes_Expired_Consumers()
        {
            var registry = CreateRegistry(out _);
            registry.Register("testQueue", "MACHINE1", 1234, null);

            // With a zero threshold, everything is stale
            var pruned = registry.PruneStale(TimeSpan.Zero);
            pruned.Should().Be(1);
            registry.GetAll().Should().BeEmpty();
        }

        [TestMethod]
        public void PruneStale_Keeps_Fresh_Consumers()
        {
            var registry = CreateRegistry(out _);
            registry.Register("testQueue", "MACHINE1", 1234, null);

            var pruned = registry.PruneStale(TimeSpan.FromMinutes(5));
            pruned.Should().Be(0);
            registry.GetAll().Should().HaveCount(1);
        }

        [TestMethod]
        public void PruneStale_Returns_Zero_When_Empty()
        {
            var registry = CreateRegistry(out _);
            registry.PruneStale(TimeSpan.Zero).Should().Be(0);
        }
    }
}
