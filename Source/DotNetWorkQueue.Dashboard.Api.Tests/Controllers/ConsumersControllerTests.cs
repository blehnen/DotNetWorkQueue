using System;
using System.Linq;
using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using DotNetWorkQueue.Dashboard.Api.Controllers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Dashboard.Api.Services;
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

            Assert.IsInstanceOfType<ObjectResult>(result);
            var objectResult = (ObjectResult)result;
            Assert.AreEqual(201, objectResult.StatusCode);
            Assert.IsInstanceOfType<ConsumerRegistrationResponse>(objectResult.Value);
            var response = (ConsumerRegistrationResponse)objectResult.Value;
            Assert.AreEqual(consumerId, response.ConsumerId);
        }

        [TestMethod]
        public void Register_Returns_NotFound_When_Tracking_Disabled()
        {
            var (controller, _, _) = Create(enableTracking: false);

            var result = controller.Register(new ConsumerRegistrationRequest
            {
                QueueName = "q", MachineName = "M", ProcessId = 1
            });

            Assert.IsInstanceOfType<NotFoundResult>(result);
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
            Assert.AreEqual(45, response!.HeartbeatIntervalSeconds);
        }

        // === Heartbeat ===

        [TestMethod]
        public void Heartbeat_Returns_NoContent_When_Found()
        {
            var (controller, registry, _) = Create();
            var id = Guid.NewGuid();
            registry.Heartbeat(id, Arg.Any<long>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<long>()).Returns(true);

            var result = controller.Heartbeat(new ConsumerHeartbeatRequest { ConsumerId = id });

            Assert.IsInstanceOfType<NoContentResult>(result);
        }

        [TestMethod]
        public void Heartbeat_Returns_NotFound_When_Unknown()
        {
            var (controller, registry, _) = Create();
            registry.Heartbeat(Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<long>()).Returns(false);

            var result = controller.Heartbeat(new ConsumerHeartbeatRequest { ConsumerId = Guid.NewGuid() });

            Assert.IsInstanceOfType<NotFoundResult>(result);
        }

        [TestMethod]
        public void Heartbeat_Passes_Metrics_To_Registry()
        {
            var (controller, registry, _) = Create();
            var id = Guid.NewGuid();
            registry.Heartbeat(id, 100, 5, 3, 1).Returns(true);

            var result = controller.Heartbeat(new ConsumerHeartbeatRequest
            {
                ConsumerId = id,
                MessagesProcessed = 100,
                MessagesErrored = 5,
                MessagesRolledBack = 3,
                PoisonMessages = 1
            });

            Assert.IsInstanceOfType<NoContentResult>(result);
            registry.Received(1).Heartbeat(id, 100, 5, 3, 1);
        }

        [TestMethod]
        public void Heartbeat_Returns_NotFound_When_Tracking_Disabled()
        {
            var (controller, _, _) = Create(enableTracking: false);

            var result = controller.Heartbeat(new ConsumerHeartbeatRequest { ConsumerId = Guid.NewGuid() });

            Assert.IsInstanceOfType<NotFoundResult>(result);
        }

        // === Unregister ===

        [TestMethod]
        public void Unregister_Returns_NoContent_When_Found()
        {
            var (controller, registry, _) = Create();
            var id = Guid.NewGuid();
            registry.Unregister(id).Returns(true);

            var result = controller.Unregister(id);

            Assert.IsInstanceOfType<NoContentResult>(result);
        }

        [TestMethod]
        public void Unregister_Returns_NotFound_When_Unknown()
        {
            var (controller, registry, _) = Create();
            registry.Unregister(Arg.Any<Guid>()).Returns(false);

            var result = controller.Unregister(Guid.NewGuid());

            Assert.IsInstanceOfType<NotFoundResult>(result);
        }

        [TestMethod]
        public void Unregister_Returns_NotFound_When_Tracking_Disabled()
        {
            var (controller, _, _) = Create(enableTracking: false);

            var result = controller.Unregister(Guid.NewGuid());

            Assert.IsInstanceOfType<NotFoundResult>(result);
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

            Assert.IsNotNull(result);
            var items = result!.Value as List<ConsumerInfoResponse>;
            Assert.HasCount(1, items);
        }

        [TestMethod]
        public void GetConsumers_Returns_Metrics_In_Response()
        {
            var (controller, registry, _) = Create();
            registry.GetAll().Returns(new List<ConsumerEntry>
            {
                new ConsumerEntry
                {
                    ConsumerId = Guid.NewGuid(),
                    MachineName = "M1",
                    MessagesProcessed = 500,
                    MessagesErrored = 10,
                    MessagesRolledBack = 7,
                    PoisonMessages = 2
                }
            });

            var result = controller.GetConsumers() as OkObjectResult;
            var items = result!.Value as List<ConsumerInfoResponse>;
            Assert.HasCount(1, items);
            Assert.AreEqual(500, items![0].MessagesProcessed);
            Assert.AreEqual(10, items[0].MessagesErrored);
            Assert.AreEqual(7, items[0].MessagesRolledBack);
            Assert.AreEqual(2, items[0].PoisonMessages);
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

            Assert.IsNotNull(result);
            var items = result!.Value as ConsumerInfoResponse[];
            Assert.IsEmpty(items);
        }

        // === GetConsumerCounts ===

        [TestMethod]
        public void GetConsumerCounts_Returns_Ok_With_Dictionary()
        {
            var (controller, registry, _) = Create();
            var queueId = Guid.NewGuid();
            registry.GetCountsByQueue().Returns(new Dictionary<Guid, int> { { queueId, 3 } });

            var result = controller.GetConsumerCounts() as OkObjectResult;

            Assert.IsNotNull(result);
            var counts = result!.Value as Dictionary<Guid, int>;
            Assert.IsTrue((counts).ContainsKey(queueId));
            Assert.AreEqual(3, counts![queueId]);
        }

        [TestMethod]
        public void GetConsumerCounts_Returns_Empty_When_Tracking_Disabled()
        {
            var (controller, _, _) = Create(enableTracking: false);

            var result = controller.GetConsumerCounts() as OkObjectResult;

            Assert.IsNotNull(result);
            var counts = result!.Value as Dictionary<Guid, int>;
            Assert.IsEmpty(counts);
        }
    }
}
