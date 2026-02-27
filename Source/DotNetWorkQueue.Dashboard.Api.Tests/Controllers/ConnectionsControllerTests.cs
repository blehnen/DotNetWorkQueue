using System;
using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Api.Controllers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Dashboard.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Controllers
{
    public class ConnectionsControllerTests
    {
        [Fact]
        public void GetConnections_Returns_Ok_With_List()
        {
            var service = Substitute.For<IDashboardService>();
            service.GetConnections().Returns(new List<ConnectionResponse>());
            var controller = new ConnectionsController(service);

            var result = controller.GetConnections();

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
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

        [Fact]
        public void GetQueues_Returns_Ok_With_List()
        {
            var service = Substitute.For<IDashboardService>();
            var connectionId = Guid.NewGuid();
            service.GetQueues(connectionId).Returns(new List<QueueInfoResponse>());
            var controller = new ConnectionsController(service);

            var result = controller.GetQueues(connectionId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetQueues_Calls_Service_With_ConnectionId()
        {
            var service = Substitute.For<IDashboardService>();
            var connectionId = Guid.NewGuid();
            service.GetQueues(connectionId).Returns(new List<QueueInfoResponse>());
            var controller = new ConnectionsController(service);

            controller.GetQueues(connectionId);

            service.Received(1).GetQueues(connectionId);
        }

        [Fact]
        public void GetJobs_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var connectionId = Guid.NewGuid();
            service.GetJobsByConnection(connectionId).Returns(new List<JobResponse>());
            var controller = new ConnectionsController(service);

            var result = controller.GetJobs(connectionId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetJobs_Calls_Service_With_ConnectionId()
        {
            var service = Substitute.For<IDashboardService>();
            var connectionId = Guid.NewGuid();
            service.GetJobsByConnection(connectionId).Returns(new List<JobResponse>());
            var controller = new ConnectionsController(service);

            controller.GetJobs(connectionId);

            service.Received(1).GetJobsByConnection(connectionId);
        }
    }
}
