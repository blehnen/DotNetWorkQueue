using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        public async Task GetStatus_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetStatusAsync(queueId).Returns(Task.FromResult(new QueueStatusResponse
            {
                Waiting = 10, Processing = 5, Error = 2, Total = 17
            }));
            var controller = new QueuesController(service);

            var result = await controller.GetStatus(queueId);

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
        public async Task GetMessages_Passes_Parameters()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessagesAsync(queueId, 2, 50, 0).Returns(Task.FromResult(new PagedResponse<MessageResponse>
            {
                Items = new List<MessageResponse>(), TotalCount = 100, PageIndex = 2, PageSize = 50
            }));
            var controller = new QueuesController(service);

            await controller.GetMessages(queueId, pageIndex: 2, pageSize: 50, status: 0);

            await service.Received(1).GetMessagesAsync(queueId, 2, 50, 0);
        }

        [Fact]
        public async Task GetMessageCount_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessageCountAsync(queueId, null).Returns(Task.FromResult(42L));
            var controller = new QueuesController(service);

            var result = await controller.GetMessageCount(queueId);

            result.Should().BeOfType<OkObjectResult>();
            ((OkObjectResult)result).Value.Should().Be(42L);
        }

        [Fact]
        public async Task GetMessageDetail_Returns_Ok_When_Found()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessageDetailAsync(queueId, "1").Returns(Task.FromResult(new MessageResponse { QueueId = "1" }));
            var controller = new QueuesController(service);

            var result = await controller.GetMessageDetail(queueId, "1");

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetMessageDetail_Returns_NotFound_When_Null()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessageDetailAsync(queueId, "999").Returns(Task.FromResult((MessageResponse)null));
            var controller = new QueuesController(service);

            var result = await controller.GetMessageDetail(queueId, "999");

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetStaleMessages_Uses_Default_Threshold()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetStaleMessagesAsync(queueId, 60, 0, 25).Returns(Task.FromResult(new PagedResponse<MessageResponse>
            {
                Items = new List<MessageResponse>(), TotalCount = 0, PageIndex = 0, PageSize = 25
            }));
            var controller = new QueuesController(service);

            await controller.GetStaleMessages(queueId);

            await service.Received(1).GetStaleMessagesAsync(queueId, 60, 0, 25);
        }

        [Fact]
        public async Task GetErrors_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetErrorsAsync(queueId, 0, 25).Returns(Task.FromResult(new PagedResponse<ErrorMessageResponse>
            {
                Items = new List<ErrorMessageResponse>(), TotalCount = 0, PageIndex = 0, PageSize = 25
            }));
            var controller = new QueuesController(service);

            var result = await controller.GetErrors(queueId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetErrorRetries_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetErrorRetriesAsync(queueId, "1").Returns(Task.FromResult<IReadOnlyList<ErrorRetryResponse>>(new List<ErrorRetryResponse>()));
            var controller = new QueuesController(service);

            var result = await controller.GetErrorRetries(queueId, "1");

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetConfiguration_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetConfigurationAsync(queueId).Returns(Task.FromResult(new ConfigurationResponse { ConfigurationJson = "{}" }));
            var controller = new QueuesController(service);

            var result = await controller.GetConfiguration(queueId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetMessages_Returns_BadRequest_For_Invalid_Status()
        {
            var service = Substitute.For<IDashboardService>();
            var controller = new QueuesController(service);

            var result = await controller.GetMessages(Guid.NewGuid(), status: 99);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetMessages_Accepts_Valid_Status()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessagesAsync(queueId, 0, 25, 0).Returns(Task.FromResult(new PagedResponse<MessageResponse>
            {
                Items = new List<MessageResponse>(), TotalCount = 0, PageIndex = 0, PageSize = 25
            }));
            var controller = new QueuesController(service);

            var result = await controller.GetMessages(queueId, status: 0);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetMessageCount_Returns_BadRequest_For_Invalid_Status()
        {
            var service = Substitute.For<IDashboardService>();
            var controller = new QueuesController(service);

            var result = await controller.GetMessageCount(Guid.NewGuid(), status: 99);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetMessages_Accepts_Null_Status()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessagesAsync(queueId, 0, 25, null).Returns(Task.FromResult(new PagedResponse<MessageResponse>
            {
                Items = new List<MessageResponse>(), TotalCount = 0, PageIndex = 0, PageSize = 25
            }));
            var controller = new QueuesController(service);

            var result = await controller.GetMessages(queueId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetMessageBody_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessageBodyAsync(queueId, "1").Returns(Task.FromResult(new MessageBodyResponse
            {
                Body = "{}", InterceptorChain = new List<string>()
            }));
            var controller = new QueuesController(service);

            var result = await controller.GetMessageBody(queueId, "1");

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetMessageBody_Returns_NotFound()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessageBodyAsync(queueId, "999").Returns(Task.FromResult((MessageBodyResponse)null));
            var controller = new QueuesController(service);

            var result = await controller.GetMessageBody(queueId, "999");

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetMessageHeaders_Returns_Ok()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessageHeadersAsync(queueId, "1").Returns(Task.FromResult(new MessageHeadersResponse
            {
                Headers = new Dictionary<string, object> { { "key", "value" } }
            }));
            var controller = new QueuesController(service);

            var result = await controller.GetMessageHeaders(queueId, "1");

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetMessageHeaders_Returns_NotFound()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.GetMessageHeadersAsync(queueId, "999").Returns(Task.FromResult((MessageHeadersResponse)null));
            var controller = new QueuesController(service);

            var result = await controller.GetMessageHeaders(queueId, "999");

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task DeleteMessage_Returns_NoContent_When_Found()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.DeleteMessageAsync(queueId, "1").Returns(Task.FromResult(true));
            var controller = new QueuesController(service);

            var result = await controller.DeleteMessage(queueId, "1");

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteMessage_Returns_NotFound_When_Not_Found()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.DeleteMessageAsync(queueId, "999").Returns(Task.FromResult(false));
            var controller = new QueuesController(service);

            var result = await controller.DeleteMessage(queueId, "999");

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task DeleteAllErrors_Returns_Deleted_Count()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.DeleteAllErrorMessagesAsync(queueId).Returns(Task.FromResult(5L));
            var controller = new QueuesController(service);

            var result = await controller.DeleteAllErrors(queueId);

            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ((DeleteAllResponse)ok.Value).Deleted.Should().Be(5L);
        }

        [Fact]
        public async Task RequeueErrorMessage_Returns_NoContent_When_Found()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.RequeueErrorMessageAsync(queueId, "1").Returns(Task.FromResult(true));
            var controller = new QueuesController(service);

            var result = await controller.RequeueErrorMessage(queueId, "1");

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task RequeueErrorMessage_Returns_NotFound_When_Not_Found()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.RequeueErrorMessageAsync(queueId, "999").Returns(Task.FromResult(false));
            var controller = new QueuesController(service);

            var result = await controller.RequeueErrorMessage(queueId, "999");

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task ResetStaleMessage_Returns_NoContent_When_Reset()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.ResetStaleMessageAsync(queueId, "1").Returns(Task.FromResult(true));
            var controller = new QueuesController(service);

            var result = await controller.ResetStaleMessage(queueId, "1");

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task ResetStaleMessage_Returns_NotFound_When_Not_In_Processing()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.ResetStaleMessageAsync(queueId, "999").Returns(Task.FromResult(false));
            var controller = new QueuesController(service);

            var result = await controller.ResetStaleMessage(queueId, "999");

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task EditMessageBody_Returns_BadRequest_When_Body_Is_Null()
        {
            var service = Substitute.For<IDashboardService>();
            var controller = new QueuesController(service);

            var result = await controller.EditMessageBody(Guid.NewGuid(), "1", new EditMessageBodyRequest { Body = null });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EditMessageBody_Returns_NoContent_On_Success()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.EditMessageBodyAsync(queueId, "1", Arg.Any<string>()).Returns(Task.FromResult(EditMessageBodyResult.Success));
            var controller = new QueuesController(service);

            var result = await controller.EditMessageBody(queueId, "1", new EditMessageBodyRequest { Body = "{}" });

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task EditMessageBody_Returns_NotFound_When_Message_Not_Found()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.EditMessageBodyAsync(queueId, "999", Arg.Any<string>()).Returns(Task.FromResult(EditMessageBodyResult.NotFound));
            var controller = new QueuesController(service);

            var result = await controller.EditMessageBody(queueId, "999", new EditMessageBodyRequest { Body = "{}" });

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task EditMessageBody_Returns_BadRequest_When_TypeUnresolvable()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.EditMessageBodyAsync(queueId, "1", Arg.Any<string>()).Returns(Task.FromResult(EditMessageBodyResult.TypeUnresolvable));
            var controller = new QueuesController(service);

            var result = await controller.EditMessageBody(queueId, "1", new EditMessageBodyRequest { Body = "{}" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EditMessageBody_Returns_Conflict_When_Message_Being_Processed()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.EditMessageBodyAsync(queueId, "1", Arg.Any<string>()).Returns(Task.FromResult(EditMessageBodyResult.MessageBeingProcessed));
            var controller = new QueuesController(service);

            var result = await controller.EditMessageBody(queueId, "1", new EditMessageBodyRequest { Body = "{}" });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task EditMessageBody_Returns_BadRequest_When_Invalid_Json()
        {
            var service = Substitute.For<IDashboardService>();
            var queueId = Guid.NewGuid();
            service.EditMessageBodyAsync(queueId, "1", Arg.Any<string>()).Returns(Task.FromResult(EditMessageBodyResult.InvalidJson));
            var controller = new QueuesController(service);

            var result = await controller.EditMessageBody(queueId, "1", new EditMessageBodyRequest { Body = "{invalid}" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
