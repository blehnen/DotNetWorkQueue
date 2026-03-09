using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Controllers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Dashboard.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Controllers
{
    [TestClass]
    public class ConnectionsControllerTests
    {
        [TestMethod]
        public void GetConnections_Returns_Ok_With_List()
        {
            var service = Substitute.For<IDashboardService>();
            service.GetConnections().Returns(new List<ConnectionResponse>());
            var controller = new ConnectionsController(service);

            var result = controller.GetConnections();

            result.Should().BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public void GetConnections_Returns_Populated_List()
        {
            var service = Substitute.For<IDashboardService>();
            var connections = new List<ConnectionResponse>
            {
                new ConnectionResponse { Id = Guid.NewGuid(), DisplayName = "Test Connection", QueueCount = 2 }
            };
            service.GetConnections().Returns(connections);
            var controller = new ConnectionsController(service);

            var result = controller.GetConnections() as OkObjectResult;

            result.Should().NotBeNull();
            var items = result.Value as IReadOnlyList<ConnectionResponse>;
            items.Should().HaveCount(1);
        }

        [TestMethod]
        public void GetQueues_Returns_Ok_With_List()
        {
            var service = Substitute.For<IDashboardService>();
            var connectionId = Guid.NewGuid();
            service.GetQueues(connectionId).Returns(new List<QueueInfoResponse>());
            var controller = new ConnectionsController(service);

            var result = controller.GetQueues(connectionId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public void GetQueues_Calls_Service_With_ConnectionId()
        {
            var service = Substitute.For<IDashboardService>();
            var connectionId = Guid.NewGuid();
            service.GetQueues(connectionId).Returns(new List<QueueInfoResponse>());
            var controller = new ConnectionsController(service);

            controller.GetQueues(connectionId);

            service.Received(1).GetQueues(connectionId);
        }

        [TestMethod]
        public async Task GetJobs_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var connectionId = Guid.NewGuid();
            service.GetJobsByConnectionAsync(connectionId).Returns(Task.FromResult<IReadOnlyList<JobResponse>>(new List<JobResponse>()));
            var controller = new ConnectionsController(service);

            var result = await controller.GetJobs(connectionId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetJobs_Calls_Service_With_ConnectionId()
        {
            var service = Substitute.For<IDashboardService>();
            var connectionId = Guid.NewGuid();
            service.GetJobsByConnectionAsync(connectionId).Returns(Task.FromResult<IReadOnlyList<JobResponse>>(new List<JobResponse>()));
            var controller = new ConnectionsController(service);

            await controller.GetJobs(connectionId);

            await service.Received(1).GetJobsByConnectionAsync(connectionId);
        }
    }
}
