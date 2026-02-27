using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task GetStatus_Returns_StatusCounts()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardStatusCountsQuery, DashboardStatusCounts>>();
            handler.HandleAsync(Arg.Any<GetDashboardStatusCountsQuery>()).Returns(Task.FromResult(new DashboardStatusCounts
            {
                Waiting = 10, Processing = 5, Error = 2, Total = 17
            }));
            container.GetInstance<IQueryHandlerAsync<GetDashboardStatusCountsQuery, DashboardStatusCounts>>().Returns(handler);

            var service = new DashboardService(api);
            var result = await service.GetStatusAsync(queueId);

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
        public async Task GetMessageCount_Calls_Handler()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardMessageCountQuery, long>>();
            handler.HandleAsync(Arg.Any<GetDashboardMessageCountQuery>()).Returns(Task.FromResult(42L));
            container.GetInstance<IQueryHandlerAsync<GetDashboardMessageCountQuery, long>>().Returns(handler);

            var service = new DashboardService(api);
            var result = await service.GetMessageCountAsync(queueId, null);

            result.Should().Be(42L);
        }

        [Fact]
        public async Task GetMessageDetail_Returns_Null_When_Not_Found()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardMessageDetailQuery, DashboardMessage>>();
            handler.HandleAsync(Arg.Any<GetDashboardMessageDetailQuery>()).Returns(Task.FromResult((DashboardMessage)null));
            container.GetInstance<IQueryHandlerAsync<GetDashboardMessageDetailQuery, DashboardMessage>>().Returns(handler);

            var service = new DashboardService(api);
            var result = await service.GetMessageDetailAsync(queueId, 999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetJobs_Returns_Mapped_Jobs()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            SetupJobTableExists(container, true);

            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            handler.HandleAsync(Arg.Any<GetDashboardJobsQuery>()).Returns(Task.FromResult<IReadOnlyList<DashboardJob>>(new List<DashboardJob>
            {
                new DashboardJob { JobName = "TestJob", JobEventTime = DateTimeOffset.UtcNow }
            }));
            container.GetInstance<IQueryHandlerAsync<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>().Returns(handler);

            var service = new DashboardService(api);
            var result = await service.GetJobsAsync(queueId);

            result.Should().HaveCount(1);
            result[0].JobName.Should().Be("TestJob");
        }

        [Fact]
        public async Task GetJobs_Returns_Empty_When_Table_Does_Not_Exist()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            SetupJobTableExists(container, false);

            var service = new DashboardService(api);
            var result = await service.GetJobsAsync(queueId);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetConfiguration_Returns_Utf8_Json()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardConfigurationQuery, byte[]>>();
            handler.HandleAsync(Arg.Any<GetDashboardConfigurationQuery>()).Returns(
                Task.FromResult(System.Text.Encoding.UTF8.GetBytes("{\"test\":true}")));
            container.GetInstance<IQueryHandlerAsync<GetDashboardConfigurationQuery, byte[]>>().Returns(handler);

            var service = new DashboardService(api);
            var result = await service.GetConfigurationAsync(queueId);

            result.ConfigurationJson.Should().Be("{\"test\":true}");
        }

        [Fact]
        public async Task GetJobsByConnection_Returns_Jobs_From_First_Queue()
        {
            var api = CreateApi(out var connectionId, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            SetupJobTableExists(container, true);

            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            handler.HandleAsync(Arg.Any<GetDashboardJobsQuery>()).Returns(Task.FromResult<IReadOnlyList<DashboardJob>>(new List<DashboardJob>
            {
                new DashboardJob { JobName = "ConnectionJob" }
            }));
            container.GetInstance<IQueryHandlerAsync<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>().Returns(handler);

            var service = new DashboardService(api);
            var result = await service.GetJobsByConnectionAsync(connectionId);

            result.Should().HaveCount(1);
            result[0].JobName.Should().Be("ConnectionJob");
        }

        [Fact]
        public async Task GetJobsByConnection_Returns_Empty_When_No_Queues()
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
            var result = await service.GetJobsByConnectionAsync(connectionId);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetJobsByConnection_Throws_For_Unknown_ConnectionId()
        {
            var api = CreateApi(out _, out _);
            var service = new DashboardService(api);

            var act = async () => await service.GetJobsByConnectionAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<InvalidOperationException>();
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
