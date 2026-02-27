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
    public class QueuesControllerTests
    {
        [Fact]
        public void GetStatus_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetStatus(queueId).Returns(new QueueStatusResponse
            {
                Waiting = 10, Processing = 5, Error = 2, Total = 17
            });
            var controller = new QueuesController(service);

            var result = controller.GetStatus(queueId);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)result;
            ((QueueStatusResponse)okResult.Value).Total.Should().Be(17);
        }

        [Fact]
        public void GetFeatures_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetFeatures(queueId).Returns(new QueueFeaturesResponse { EnableStatus = true });
            var controller = new QueuesController(service);

            var result = controller.GetFeatures(queueId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetMessages_Passes_Parameters()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessages(queueId, 2, 50, 0).Returns(new PagedResponse<MessageResponse>
            {
                Items = new List<MessageResponse>(), TotalCount = 100, PageIndex = 2, PageSize = 50
            });
            var controller = new QueuesController(service);

            controller.GetMessages(queueId, pageIndex: 2, pageSize: 50, status: 0);

            service.Received(1).GetMessages(queueId, 2, 50, 0);
        }

        [Fact]
        public void GetMessageCount_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessageCount(queueId, null).Returns(42L);
            var controller = new QueuesController(service);

            var result = controller.GetMessageCount(queueId);

            result.Should().BeOfType<OkObjectResult>();
            ((OkObjectResult)result).Value.Should().Be(42L);
        }

        [Fact]
        public void GetMessageDetail_Returns_Ok_When_Found()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessageDetail(queueId, 1).Returns(new MessageResponse { QueueId = 1 });
            var controller = new QueuesController(service);

            var result = controller.GetMessageDetail(queueId, 1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetMessageDetail_Returns_NotFound_When_Null()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessageDetail(queueId, 999).Returns((MessageResponse)null);
            var controller = new QueuesController(service);

            var result = controller.GetMessageDetail(queueId, 999);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void GetStaleMessages_Uses_Default_Threshold()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetStaleMessages(queueId, 60, 0, 25).Returns(new PagedResponse<MessageResponse>
            {
                Items = new List<MessageResponse>(), TotalCount = 0, PageIndex = 0, PageSize = 25
            });
            var controller = new QueuesController(service);

            controller.GetStaleMessages(queueId);

            service.Received(1).GetStaleMessages(queueId, 60, 0, 25);
        }

        [Fact]
        public void GetErrors_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetErrors(queueId, 0, 25).Returns(new PagedResponse<ErrorMessageResponse>
            {
                Items = new List<ErrorMessageResponse>(), TotalCount = 0, PageIndex = 0, PageSize = 25
            });
            var controller = new QueuesController(service);

            var result = controller.GetErrors(queueId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetErrorRetries_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetErrorRetries(queueId, 1).Returns(new List<ErrorRetryResponse>());
            var controller = new QueuesController(service);

            var result = controller.GetErrorRetries(queueId, 1);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetConfiguration_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetConfiguration(queueId).Returns(new ConfigurationResponse { ConfigurationJson = "{}" });
            var controller = new QueuesController(service);

            var result = controller.GetConfiguration(queueId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetMessages_Returns_BadRequest_For_Invalid_Status()
        {
            var service = Substitute.For<IDashboardService>();
            var controller = new QueuesController(service);

            var result = controller.GetMessages(Guid.NewGuid(), status: 99);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void GetMessages_Accepts_Valid_Status()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessages(queueId, 0, 25, 0).Returns(new PagedResponse<MessageResponse>
            {
                Items = new List<MessageResponse>(), TotalCount = 0, PageIndex = 0, PageSize = 25
            });
            var controller = new QueuesController(service);

            var result = controller.GetMessages(queueId, status: 0);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetMessageCount_Returns_BadRequest_For_Invalid_Status()
        {
            var service = Substitute.For<IDashboardService>();
            var controller = new QueuesController(service);

            var result = controller.GetMessageCount(Guid.NewGuid(), status: 99);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void GetMessages_Accepts_Null_Status()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessages(queueId, 0, 25, null).Returns(new PagedResponse<MessageResponse>
            {
                Items = new List<MessageResponse>(), TotalCount = 0, PageIndex = 0, PageSize = 25
            });
            var controller = new QueuesController(service);

            var result = controller.GetMessages(queueId);

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}
