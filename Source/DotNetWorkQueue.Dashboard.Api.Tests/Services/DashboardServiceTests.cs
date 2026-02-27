using System;
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Dashboard.Api.Services;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Services
{
    public class DashboardServiceTests
    {
        [Fact]
        public void GetConnections_Returns_All_Registered_Connections()
        {
            var api = CreateApi(out _, out _);
            var service = new DashboardService(api);

            var result = service.GetConnections();

            result.Should().HaveCount(1);
            result[0].DisplayName.Should().Be("Test Connection");
            result[0].QueueCount.Should().Be(1);
        }

        [Fact]
        public void GetQueues_Returns_Queues_For_Connection()
        {
            var api = CreateApi(out var connectionId, out _);
            var service = new DashboardService(api);

            var result = service.GetQueues(connectionId);

            result.Should().HaveCount(1);
            result[0].QueueName.Should().Be("TestQueue");
        }

        [Fact]
        public void GetQueues_Throws_For_Unknown_ConnectionId()
        {
            var api = CreateApi(out _, out _);
            var service = new DashboardService(api);

            var act = () => service.GetQueues(Guid.NewGuid());

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetStatus_Returns_StatusCounts()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<IQueryHandler<GetDashboardStatusCountsQuery, DashboardStatusCounts>>();
            handler.Handle(Arg.Any<GetDashboardStatusCountsQuery>()).Returns(new DashboardStatusCounts
            {
                Waiting = 10, Processing = 5, Error = 2, Total = 17
            });
            container.GetInstance<IQueryHandler<GetDashboardStatusCountsQuery, DashboardStatusCounts>>().Returns(handler);

            var service = new DashboardService(api);
            var result = service.GetStatus(queueId);

            result.Waiting.Should().Be(10);
            result.Processing.Should().Be(5);
            result.Error.Should().Be(2);
            result.Total.Should().Be(17);
        }

        [Fact]
        public void GetFeatures_Returns_TransportOptions()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var factory = Substitute.For<ITransportOptionsFactory>();
            var options = Substitute.For<ITransportOptions>();
            options.EnableStatus.Returns(true);
            options.EnablePriority.Returns(false);
            options.EnableHeartBeat.Returns(true);
            options.EnableDelayedProcessing.Returns(false);
            options.EnableMessageExpiration.Returns(true);
            options.EnableRoute.Returns(false);
            options.EnableStatusTable.Returns(true);
            factory.Create().Returns(options);
            container.GetInstance<ITransportOptionsFactory>().Returns(factory);

            var service = new DashboardService(api);
            var result = service.GetFeatures(queueId);

            result.EnableStatus.Should().BeTrue();
            result.EnablePriority.Should().BeFalse();
            result.EnableHeartBeat.Should().BeTrue();
            result.EnableStatusTable.Should().BeTrue();
        }

        [Fact]
        public void GetMessageCount_Calls_Handler()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<IQueryHandler<GetDashboardMessageCountQuery, long>>();
            handler.Handle(Arg.Any<GetDashboardMessageCountQuery>()).Returns(42L);
            container.GetInstance<IQueryHandler<GetDashboardMessageCountQuery, long>>().Returns(handler);

            var service = new DashboardService(api);
            var result = service.GetMessageCount(queueId, null);

            result.Should().Be(42L);
        }

        [Fact]
        public void GetMessageDetail_Returns_Null_When_Not_Found()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<IQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage>>();
            handler.Handle(Arg.Any<GetDashboardMessageDetailQuery>()).Returns((DashboardMessage)null);
            container.GetInstance<IQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage>>().Returns(handler);

            var service = new DashboardService(api);
            var result = service.GetMessageDetail(queueId, 999);

            result.Should().BeNull();
        }

        [Fact]
        public void GetJobs_Returns_Mapped_Jobs()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            SetupJobTableExists(container, true);

            var handler = Substitute.For<IQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            handler.Handle(Arg.Any<GetDashboardJobsQuery>()).Returns(new List<DashboardJob>
            {
                new DashboardJob { JobName = "TestJob", JobEventTime = DateTimeOffset.UtcNow }
            });
            container.GetInstance<IQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>().Returns(handler);

            var service = new DashboardService(api);
            var result = service.GetJobs(queueId);

            result.Should().HaveCount(1);
            result[0].JobName.Should().Be("TestJob");
        }

        [Fact]
        public void GetJobs_Returns_Empty_When_Table_Does_Not_Exist()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            SetupJobTableExists(container, false);

            var service = new DashboardService(api);
            var result = service.GetJobs(queueId);

            result.Should().BeEmpty();
        }

        [Fact]
        public void GetConfiguration_Returns_Utf8_Json()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<IQueryHandler<GetDashboardConfigurationQuery, byte[]>>();
            handler.Handle(Arg.Any<GetDashboardConfigurationQuery>()).Returns(
                System.Text.Encoding.UTF8.GetBytes("{\"test\":true}"));
            container.GetInstance<IQueryHandler<GetDashboardConfigurationQuery, byte[]>>().Returns(handler);

            var service = new DashboardService(api);
            var result = service.GetConfiguration(queueId);

            result.ConfigurationJson.Should().Be("{\"test\":true}");
        }

        [Fact]
        public void GetJobsByConnection_Returns_Jobs_From_First_Queue()
        {
            var api = CreateApi(out var connectionId, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            SetupJobTableExists(container, true);

            var handler = Substitute.For<IQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            handler.Handle(Arg.Any<GetDashboardJobsQuery>()).Returns(new List<DashboardJob>
            {
                new DashboardJob { JobName = "ConnectionJob" }
            });
            container.GetInstance<IQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>().Returns(handler);

            var service = new DashboardService(api);
            var result = service.GetJobsByConnection(connectionId);

            result.Should().HaveCount(1);
            result[0].JobName.Should().Be("ConnectionJob");
        }

        [Fact]
        public void GetJobsByConnection_Returns_Empty_When_No_Queues()
        {
            var connectionId = Guid.NewGuid();
            var connectionInfo = new DashboardConnectionInfo
            {
                Id = connectionId,
                ConnectionString = "Server=test",
                DisplayName = "Empty Connection",
                Queues = new List<DashboardQueueInfo>()
            };

            var api = Substitute.For<IDashboardApi>();
            api.Connections.Returns(new Dictionary<Guid, DashboardConnectionInfo>
            {
                { connectionId, connectionInfo }
            });

            var service = new DashboardService(api);
            var result = service.GetJobsByConnection(connectionId);

            result.Should().BeEmpty();
        }

        [Fact]
        public void GetJobsByConnection_Throws_For_Unknown_ConnectionId()
        {
            var api = CreateApi(out _, out _);
            var service = new DashboardService(api);

            var act = () => service.GetJobsByConnection(Guid.NewGuid());

            act.Should().Throw<InvalidOperationException>();
        }

        private static IDashboardApi CreateApi(out Guid connectionId, out Guid queueId)
        {
            connectionId = Guid.NewGuid();
            queueId = Guid.NewGuid();

            var queueInfo = new DashboardQueueInfo
            {
                Id = queueId,
                ConnectionId = connectionId,
                QueueName = "TestQueue",
                ConnectionString = "Server=test"
            };

            var connectionInfo = new DashboardConnectionInfo
            {
                Id = connectionId,
                ConnectionString = "Server=test",
                DisplayName = "Test Connection",
                Queues = new List<DashboardQueueInfo> { queueInfo }
            };

            var api = Substitute.For<IDashboardApi>();
            api.Connections.Returns(new Dictionary<Guid, DashboardConnectionInfo>
            {
                { connectionId, connectionInfo }
            });
            api.FindQueue(queueId).Returns(queueInfo);

            return api;
        }

        private static void SetupJobTableExists(IContainer container, bool exists)
        {
            var tableExistsHandler = Substitute.For<IQueryHandler<GetTableExistsQuery, bool>>();
            tableExistsHandler.Handle(Arg.Any<GetTableExistsQuery>()).Returns(exists);
            container.GetInstance<IQueryHandler<GetTableExistsQuery, bool>>().Returns(tableExistsHandler);

            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.ConnectionString.Returns("Server=test");
            container.GetInstance<IConnectionInformation>().Returns(connInfo);

            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.JobTableName.Returns("JobTable");
            container.GetInstance<ITableNameHelper>().Returns(tableNameHelper);
        }
    }
}
