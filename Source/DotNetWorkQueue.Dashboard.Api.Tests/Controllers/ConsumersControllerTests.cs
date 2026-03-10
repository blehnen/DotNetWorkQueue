using System;
using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using DotNetWorkQueue.Dashboard.Api.Controllers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Dashboard.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Controllers
{
    [TestClass]
    public class ConsumersControllerTests
    {
        private static (ConsumersController controller, IConsumerRegistry registry, DashboardOptions options) Create(bool enableTracking = true)
        {
            var registry = Substitute.For<IConsumerRegistry>();
            var options = new DashboardOptions { EnableConsumerTracking = enableTracking };
            var controller = new ConsumersController(registry, options);
            return (controller, registry, options);
        }

        // === Register ===

        [TestMethod]
        public void Register_Returns_201_With_ConsumerId()
        {
            var (controller, registry, _) = Create();
            var consumerId = Guid.NewGuid();
            registry.Register("q", "MACHINE", 1234, "test")
                .Returns(consumerId);

            var result = controller.Register(new ConsumerRegistrationRequest
            {
                QueueName = "q",
                MachineName = "MACHINE",
                ProcessId = 1234,
                FriendlyName = "test"
            });

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(201);
            var response = objectResult.Value.Should().BeOfType<ConsumerRegistrationResponse>().Subject;
            response.ConsumerId.Should().Be(consumerId);
        }

        [TestMethod]
        public void Register_Returns_NotFound_When_Tracking_Disabled()
        {
            var (controller, _, _) = Create(enableTracking: false);

            var result = controller.Register(new ConsumerRegistrationRequest
            {
                QueueName = "q", MachineName = "M", ProcessId = 1
            });

            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public void Register_Response_Includes_HeartbeatInterval()
        {
            var (controller, registry, options) = Create();
            options.ConsumerHeartbeatIntervalSeconds = 45;
            registry.Register(default, default, default, default).ReturnsForAnyArgs(Guid.NewGuid());

            var result = controller.Register(new ConsumerRegistrationRequest
            {
                QueueName = "q", MachineName = "M", ProcessId = 1
            }) as ObjectResult;

            var response = result!.Value as ConsumerRegistrationResponse;
            response!.HeartbeatIntervalSeconds.Should().Be(45);
        }

        // === Heartbeat ===

        [TestMethod]
        public void Heartbeat_Returns_NoContent_When_Found()
        {
            var (controller, registry, _) = Create();
            var id = Guid.NewGuid();
            registry.Heartbeat(id).Returns(true);

            var result = controller.Heartbeat(new ConsumerHeartbeatRequest { ConsumerId = id });

            result.Should().BeOfType<NoContentResult>();
        }

        [TestMethod]
        public void Heartbeat_Returns_NotFound_When_Unknown()
        {
            var (controller, registry, _) = Create();
            registry.Heartbeat(Arg.Any<Guid>()).Returns(false);

            var result = controller.Heartbeat(new ConsumerHeartbeatRequest { ConsumerId = Guid.NewGuid() });

            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public void Heartbeat_Returns_NotFound_When_Tracking_Disabled()
        {
            var (controller, _, _) = Create(enableTracking: false);

            var result = controller.Heartbeat(new ConsumerHeartbeatRequest { ConsumerId = Guid.NewGuid() });

            result.Should().BeOfType<NotFoundResult>();
        }

        // === Unregister ===

        [TestMethod]
        public void Unregister_Returns_NoContent_When_Found()
        {
            var (controller, registry, _) = Create();
            var id = Guid.NewGuid();
            registry.Unregister(id).Returns(true);

            var result = controller.Unregister(id);

            result.Should().BeOfType<NoContentResult>();
        }

        [TestMethod]
        public void Unregister_Returns_NotFound_When_Unknown()
        {
            var (controller, registry, _) = Create();
            registry.Unregister(Arg.Any<Guid>()).Returns(false);

            var result = controller.Unregister(Guid.NewGuid());

            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public void Unregister_Returns_NotFound_When_Tracking_Disabled()
        {
            var (controller, _, _) = Create(enableTracking: false);

            var result = controller.Unregister(Guid.NewGuid());

            result.Should().BeOfType<NotFoundResult>();
        }

        // === GetConsumers ===

        [TestMethod]
        public void GetConsumers_Returns_Ok_With_All()
        {
            var (controller, registry, _) = Create();
            registry.GetAll().Returns(new List<ConsumerEntry>
            {
                new ConsumerEntry { ConsumerId = Guid.NewGuid(), MachineName = "M1" }
            });

            var result = controller.GetConsumers() as OkObjectResult;

            result.Should().NotBeNull();
            var items = result!.Value as List<ConsumerInfoResponse>;
            items.Should().HaveCount(1);
        }

        [TestMethod]
        public void GetConsumers_Filters_By_QueueId()
        {
            var (controller, registry, _) = Create();
            var queueId = Guid.NewGuid();
            registry.GetByQueue(queueId).Returns(new List<ConsumerEntry>());

            controller.GetConsumers(queueId);

            registry.Received(1).GetByQueue(queueId);
            registry.DidNotReceive().GetAll();
        }

        [TestMethod]
        public void GetConsumers_Returns_Empty_When_Tracking_Disabled()
        {
            var (controller, _, _) = Create(enableTracking: false);

            var result = controller.GetConsumers() as OkObjectResult;

            result.Should().NotBeNull();
            var items = result!.Value as ConsumerInfoResponse[];
            items.Should().BeEmpty();
        }

        // === GetConsumerCounts ===

        [TestMethod]
        public void GetConsumerCounts_Returns_Ok_With_Dictionary()
        {
            var (controller, registry, _) = Create();
            var queueId = Guid.NewGuid();
            registry.GetCountsByQueue().Returns(new Dictionary<Guid, int> { { queueId, 3 } });

            var result = controller.GetConsumerCounts() as OkObjectResult;

            result.Should().NotBeNull();
            var counts = result!.Value as Dictionary<Guid, int>;
            counts.Should().ContainKey(queueId);
            counts![queueId].Should().Be(3);
        }

        [TestMethod]
        public void GetConsumerCounts_Returns_Empty_When_Tracking_Disabled()
        {
            var (controller, _, _) = Create(enableTracking: false);

            var result = controller.GetConsumerCounts() as OkObjectResult;

            result.Should().NotBeNull();
            var counts = result!.Value as Dictionary<Guid, int>;
            counts.Should().BeEmpty();
        }
    }
}
