using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Dashboard.Api.Services;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Services
{
    public class DashboardServiceTests
    {
        [Fact]
        public void GetConnections_Returns_All_Registered_Connections()
        {
            var api = CreateApi(out _, out _);
            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);

            var result = service.GetConnections();

            result.Should().HaveCount(1);
            result[0].DisplayName.Should().Be("Test Connection");
            result[0].QueueCount.Should().Be(1);
        }

        [Fact]
        public void GetQueues_Returns_Queues_For_Connection()
        {
            var api = CreateApi(out var connectionId, out _);
            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);

            var result = service.GetQueues(connectionId);

            result.Should().HaveCount(1);
            result[0].QueueName.Should().Be("TestQueue");
        }

        [Fact]
        public void GetQueues_Throws_For_Unknown_ConnectionId()
        {
            var api = CreateApi(out _, out _);
            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);

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

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
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

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
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

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
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

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.GetMessageDetailAsync(queueId, 999);

            result.Should().BeNull();
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

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
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

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
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

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.GetJobsByConnectionAsync(connectionId);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetJobsByConnection_Throws_For_Unknown_ConnectionId()
        {
            var api = CreateApi(out _, out _);
            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);

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

        [Fact]
        public async Task GetMessageBody_Returns_Decoded_Body()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var bodyBytes = new byte[] { 1, 2, 3 };
            var headerBytes = new byte[] { 4, 5, 6 };

            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>();
            handler.HandleAsync(Arg.Any<GetDashboardMessageBodyQuery>()).Returns(Task.FromResult(new DashboardMessageBody
            {
                Body = bodyBytes,
                Headers = headerBytes
            }));
            container.GetInstance<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>().Returns(handler);

            var graph = new MessageInterceptorsGraph();
            var headers = new Dictionary<string, object> { { "Queue-MessageInterceptorGraph", graph } };

            var internalSerializer = Substitute.For<IInternalSerializer>();
            internalSerializer.ConvertBytesTo<IDictionary<string, object>>(headerBytes).Returns(headers);

            var serializer = Substitute.For<ASerializer>(Substitute.For<IMessageInterceptorRegistrar>());
            serializer.BytesToMessage<MessageBody>(bodyBytes, graph, Arg.Any<IDictionary<string, object>>())
                .Returns(new MessageBody { Body = "hello" });

            var compositeSerialization = Substitute.For<ICompositeSerialization>();
            compositeSerialization.InternalSerializer.Returns(internalSerializer);
            compositeSerialization.Serializer.Returns(serializer);
            container.GetInstance<ICompositeSerialization>().Returns(compositeSerialization);

            var standardHeaders = Substitute.For<IHeaders>();
            var messageInterceptorGraphData = Substitute.For<IMessageContextData<MessageInterceptorsGraph>>();
            messageInterceptorGraphData.Name.Returns("Queue-MessageInterceptorGraph");
            standardHeaders.StandardHeaders.MessageInterceptorGraph.Returns(messageInterceptorGraphData);
            container.GetInstance<IHeaders>().Returns(standardHeaders);

            container.GetInstance<IMessageFactory>().Returns(new MessageFactory());

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.GetMessageBodyAsync(queueId, 42);

            result.Should().NotBeNull();
            result.Body.Should().NotBeNullOrEmpty();
            result.TypeName.Should().Be("System.String");
            result.WasIntercepted.Should().BeFalse();
            result.InterceptorChain.Should().BeEmpty();
            result.DecodingError.Should().BeNull();
        }

        [Fact]
        public async Task GetMessageBody_Returns_Null_When_Not_Found()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>();
            handler.HandleAsync(Arg.Any<GetDashboardMessageBodyQuery>()).Returns(Task.FromResult((DashboardMessageBody)null));
            container.GetInstance<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>().Returns(handler);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.GetMessageBodyAsync(queueId, 999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetMessageBody_Returns_Error_When_Decoding_Fails()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>();
            handler.HandleAsync(Arg.Any<GetDashboardMessageBodyQuery>()).Returns(Task.FromResult(new DashboardMessageBody
            {
                Body = new byte[] { 1 },
                Headers = new byte[] { 2 }
            }));
            container.GetInstance<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>().Returns(handler);

            var internalSerializer = Substitute.For<IInternalSerializer>();
            internalSerializer.ConvertBytesTo<IDictionary<string, object>>(Arg.Any<byte[]>())
                .Throws(new Exception("Bad headers"));

            var compositeSerialization = Substitute.For<ICompositeSerialization>();
            compositeSerialization.InternalSerializer.Returns(internalSerializer);
            container.GetInstance<ICompositeSerialization>().Returns(compositeSerialization);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.GetMessageBodyAsync(queueId, 42);

            result.Should().NotBeNull();
            result.Body.Should().BeNull();
            result.DecodingError.Should().Be("Bad headers");
        }

        [Fact]
        public async Task GetMessageHeaders_Returns_Decoded_Headers()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var headerBytes = new byte[] { 4, 5, 6 };
            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>>();
            handler.HandleAsync(Arg.Any<GetDashboardMessageHeadersQuery>()).Returns(Task.FromResult(new DashboardMessageHeaders
            {
                Headers = headerBytes
            }));
            container.GetInstance<IQueryHandlerAsync<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>>().Returns(handler);

            var headers = new Dictionary<string, object> { { "key", "value" } };
            var internalSerializer = Substitute.For<IInternalSerializer>();
            internalSerializer.ConvertBytesTo<IDictionary<string, object>>(headerBytes).Returns(headers);

            var compositeSerialization = Substitute.For<ICompositeSerialization>();
            compositeSerialization.InternalSerializer.Returns(internalSerializer);
            container.GetInstance<ICompositeSerialization>().Returns(compositeSerialization);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.GetMessageHeadersAsync(queueId, 42);

            result.Should().NotBeNull();
            result.Headers.Should().ContainKey("key");
            result.DecodingError.Should().BeNull();
        }

        [Fact]
        public async Task GetMessageHeaders_Returns_Null_When_Not_Found()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>>();
            handler.HandleAsync(Arg.Any<GetDashboardMessageHeadersQuery>()).Returns(Task.FromResult((DashboardMessageHeaders)null));
            container.GetInstance<IQueryHandlerAsync<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>>().Returns(handler);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.GetMessageHeadersAsync(queueId, 999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetMessageHeaders_Returns_Error_When_Decoding_Fails()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>>();
            handler.HandleAsync(Arg.Any<GetDashboardMessageHeadersQuery>()).Returns(Task.FromResult(new DashboardMessageHeaders
            {
                Headers = new byte[] { 1 }
            }));
            container.GetInstance<IQueryHandlerAsync<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>>().Returns(handler);

            var internalSerializer = Substitute.For<IInternalSerializer>();
            internalSerializer.ConvertBytesTo<IDictionary<string, object>>(Arg.Any<byte[]>())
                .Throws(new Exception("Corrupt data"));

            var compositeSerialization = Substitute.For<ICompositeSerialization>();
            compositeSerialization.InternalSerializer.Returns(internalSerializer);
            container.GetInstance<ICompositeSerialization>().Returns(compositeSerialization);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.GetMessageHeadersAsync(queueId, 42);

            result.Should().NotBeNull();
            result.Headers.Should().BeNull();
            result.DecodingError.Should().Be("Corrupt data");
        }

        [Fact]
        public async Task GetMessageBody_Uses_TypeHeader_When_Type_Is_Resolvable()
        {
            // Use System.String — always loaded in the AppDomain, so ResolveMessageBodyType Stage 1 succeeds.
            var portableName = $"{typeof(string).FullName}, {typeof(string).Assembly.GetName().Name}";

            var api = CreateApi(out _, out var queueId);
            var container = SetupBodyDecodeContainer(queueId, api,
                new Dictionary<string, object>
                {
                    { "Queue-MessageInterceptorGraph", new MessageInterceptorsGraph() },
                    { "Queue-MessageBodyType", portableName }
                },
                bodyValue: "hello");

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.GetMessageBodyAsync(queueId, 42);

            result.Should().NotBeNull();
            result.DecodingError.Should().BeNull();
            result.TypeName.Should().Be("System.String");
        }

        [Fact]
        public async Task GetMessageBody_Falls_Back_To_JObject_When_TypeHeader_Not_Resolvable()
        {
            var api = CreateApi(out _, out var queueId);
            var container = SetupBodyDecodeContainer(queueId, api,
                new Dictionary<string, object>
                {
                    { "Queue-MessageInterceptorGraph", new MessageInterceptorsGraph() },
                    { "Queue-MessageBodyType", "NotReal.Type, NotReal" }
                },
                bodyValue: "hello");

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.GetMessageBodyAsync(queueId, 42);

            result.Should().NotBeNull();
            result.DecodingError.Should().BeNull();
            result.Body.Should().NotBeNullOrEmpty();
            // TypeName comes from the JObject/raw body since the header type couldn't be resolved
            result.TypeName.Should().Be("System.String");
        }

        [Fact]
        public async Task GetMessageBody_Falls_Back_To_JObject_When_TypeHeader_Absent()
        {
            var api = CreateApi(out _, out var queueId);
            var container = SetupBodyDecodeContainer(queueId, api,
                new Dictionary<string, object>
                {
                    { "Queue-MessageInterceptorGraph", new MessageInterceptorsGraph() }
                    // no Queue-MessageBodyType
                },
                bodyValue: "hello");

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.GetMessageBodyAsync(queueId, 42);

            result.Should().NotBeNull();
            result.DecodingError.Should().BeNull();
            result.Body.Should().NotBeNullOrEmpty();
        }

        private static IContainer SetupBodyDecodeContainer(Guid queueId, IDashboardApi api,
            Dictionary<string, object> headers, dynamic bodyValue)
        {
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var bodyBytes = new byte[] { 1, 2, 3 };
            var headerBytes = new byte[] { 4, 5, 6 };

            var handler = Substitute.For<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>();
            handler.HandleAsync(Arg.Any<GetDashboardMessageBodyQuery>()).Returns(Task.FromResult(new DashboardMessageBody
            {
                Body = bodyBytes,
                Headers = headerBytes
            }));
            container.GetInstance<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>().Returns(handler);

            var graph = (MessageInterceptorsGraph)headers["Queue-MessageInterceptorGraph"];

            var internalSerializer = Substitute.For<IInternalSerializer>();
            internalSerializer.ConvertBytesTo<IDictionary<string, object>>(headerBytes).Returns(headers);

            var serializer = Substitute.For<ASerializer>(Substitute.For<IMessageInterceptorRegistrar>());
            serializer.BytesToMessage<MessageBody>(bodyBytes, graph, Arg.Any<IDictionary<string, object>>())
                .Returns(new MessageBody { Body = bodyValue });

            var compositeSerialization = Substitute.For<ICompositeSerialization>();
            compositeSerialization.InternalSerializer.Returns(internalSerializer);
            compositeSerialization.Serializer.Returns(serializer);
            container.GetInstance<ICompositeSerialization>().Returns(compositeSerialization);

            var standardHeaders = Substitute.For<IHeaders>();
            var messageInterceptorGraphData = Substitute.For<IMessageContextData<MessageInterceptorsGraph>>();
            messageInterceptorGraphData.Name.Returns("Queue-MessageInterceptorGraph");
            standardHeaders.StandardHeaders.MessageInterceptorGraph.Returns(messageInterceptorGraphData);
            container.GetInstance<IHeaders>().Returns(standardHeaders);

            container.GetInstance<IMessageFactory>().Returns(new MessageFactory());

            return container;
        }

        [Fact]
        public async Task DeleteMessageAsync_Returns_True_When_Handler_Returns_Positive()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<ICommandHandlerWithOutput<DeleteMessageCommand<long>, long>>();
            handler.Handle(Arg.Any<DeleteMessageCommand<long>>()).Returns(1L);
            container.GetInstance<ICommandHandlerWithOutput<DeleteMessageCommand<long>, long>>().Returns(handler);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.DeleteMessageAsync(queueId, 42);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteMessageAsync_Returns_False_When_Handler_Returns_Zero()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<ICommandHandlerWithOutput<DeleteMessageCommand<long>, long>>();
            handler.Handle(Arg.Any<DeleteMessageCommand<long>>()).Returns(0L);
            container.GetInstance<ICommandHandlerWithOutput<DeleteMessageCommand<long>, long>>().Returns(handler);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.DeleteMessageAsync(queueId, 999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAllErrorMessagesAsync_Returns_Handler_Count()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<ICommandHandlerWithOutput<DashboardDeleteAllErrorMessagesCommand, long>>();
            handler.Handle(Arg.Any<DashboardDeleteAllErrorMessagesCommand>()).Returns(7L);
            container.GetInstance<ICommandHandlerWithOutput<DashboardDeleteAllErrorMessagesCommand, long>>().Returns(handler);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.DeleteAllErrorMessagesAsync(queueId);

            result.Should().Be(7L);
        }

        [Fact]
        public async Task RequeueErrorMessageAsync_Returns_True_When_Found()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<ICommandHandlerWithOutput<DashboardRequeueErrorMessageCommand, long>>();
            handler.Handle(Arg.Any<DashboardRequeueErrorMessageCommand>()).Returns(1L);
            container.GetInstance<ICommandHandlerWithOutput<DashboardRequeueErrorMessageCommand, long>>().Returns(handler);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.RequeueErrorMessageAsync(queueId, 42);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task RequeueErrorMessageAsync_Returns_False_When_Not_Found()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<ICommandHandlerWithOutput<DashboardRequeueErrorMessageCommand, long>>();
            handler.Handle(Arg.Any<DashboardRequeueErrorMessageCommand>()).Returns(0L);
            container.GetInstance<ICommandHandlerWithOutput<DashboardRequeueErrorMessageCommand, long>>().Returns(handler);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.RequeueErrorMessageAsync(queueId, 999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ResetStaleMessageAsync_Returns_True_When_Reset()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<ICommandHandlerWithOutput<DashboardResetStaleMessageCommand, long>>();
            handler.Handle(Arg.Any<DashboardResetStaleMessageCommand>()).Returns(1L);
            container.GetInstance<ICommandHandlerWithOutput<DashboardResetStaleMessageCommand, long>>().Returns(handler);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.ResetStaleMessageAsync(queueId, 42);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ResetStaleMessageAsync_Returns_False_When_Not_In_Processing()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var handler = Substitute.For<ICommandHandlerWithOutput<DashboardResetStaleMessageCommand, long>>();
            handler.Handle(Arg.Any<DashboardResetStaleMessageCommand>()).Returns(0L);
            container.GetInstance<ICommandHandlerWithOutput<DashboardResetStaleMessageCommand, long>>().Returns(handler);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.ResetStaleMessageAsync(queueId, 999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task EditMessageBodyAsync_Returns_NotFound_When_Body_Query_Returns_Null()
        {
            var api = CreateApi(out _, out var queueId);
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var bodyHandler = Substitute.For<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>();
            bodyHandler.HandleAsync(Arg.Any<GetDashboardMessageBodyQuery>()).Returns(Task.FromResult((DashboardMessageBody)null));
            container.GetInstance<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>().Returns(bodyHandler);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.EditMessageBodyAsync(queueId, 999, "{}");

            result.Should().Be(EditMessageBodyResult.NotFound);
        }

        [Fact]
        public async Task EditMessageBodyAsync_Returns_TypeUnresolvable_When_No_Type_Header()
        {
            var api = CreateApi(out _, out var queueId);
            var container = SetupEditBodyContainer(queueId, api,
                new Dictionary<string, object>
                {
                    { "Queue-MessageInterceptorGraph", new MessageInterceptorsGraph() }
                    // no Queue-MessageBodyType header
                },
                messageStatus: 0);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.EditMessageBodyAsync(queueId, 42, "\"hello\"");

            result.Should().Be(EditMessageBodyResult.TypeUnresolvable);
        }

        [Fact]
        public async Task EditMessageBodyAsync_Returns_MessageBeingProcessed_When_Status_Is_1()
        {
            var portableName = $"{typeof(string).FullName}, {typeof(string).Assembly.GetName().Name}";
            var api = CreateApi(out _, out var queueId);
            var container = SetupEditBodyContainer(queueId, api,
                new Dictionary<string, object>
                {
                    { "Queue-MessageInterceptorGraph", new MessageInterceptorsGraph() },
                    { "Queue-MessageBodyType", portableName }
                },
                messageStatus: 1);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.EditMessageBodyAsync(queueId, 42, "\"hello\"");

            result.Should().Be(EditMessageBodyResult.MessageBeingProcessed);
        }

        [Fact]
        public async Task EditMessageBodyAsync_Returns_InvalidJson_When_Json_Is_Malformed()
        {
            var portableName = $"{typeof(string).FullName}, {typeof(string).Assembly.GetName().Name}";
            var api = CreateApi(out _, out var queueId);
            var container = SetupEditBodyContainer(queueId, api,
                new Dictionary<string, object>
                {
                    { "Queue-MessageInterceptorGraph", new MessageInterceptorsGraph() },
                    { "Queue-MessageBodyType", portableName }
                },
                messageStatus: 0);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.EditMessageBodyAsync(queueId, 42, "{ not valid json");

            result.Should().Be(EditMessageBodyResult.InvalidJson);
        }

        [Fact]
        public async Task EditMessageBodyAsync_Returns_Success_When_All_Valid()
        {
            var portableName = $"{typeof(string).FullName}, {typeof(string).Assembly.GetName().Name}";
            var api = CreateApi(out _, out var queueId);
            var container = SetupEditBodyContainer(queueId, api,
                new Dictionary<string, object>
                {
                    { "Queue-MessageInterceptorGraph", new MessageInterceptorsGraph() },
                    { "Queue-MessageBodyType", portableName }
                },
                messageStatus: 0);

            var service = new DashboardService(api, NullLogger<DashboardService>.Instance);
            var result = await service.EditMessageBodyAsync(queueId, 42, "\"hello world\"");

            result.Should().Be(EditMessageBodyResult.Success);
        }

        /// <summary>
        /// Sets up a container for EditMessageBodyAsync tests. The body query handler returns
        /// non-null raw bytes, the serialization stack is mocked for the encode pipeline, and
        /// the detail query returns a message with the given <paramref name="messageStatus"/>.
        /// </summary>
        private static IContainer SetupEditBodyContainer(Guid queueId, IDashboardApi api,
            Dictionary<string, object> headers, int messageStatus)
        {
            var container = Substitute.For<IContainer>();
            api.GetQueueContainer(queueId).Returns(container);

            var bodyBytes = new byte[] { 1, 2, 3 };
            var headerBytes = new byte[] { 4, 5, 6 };
            var newHeaderBytes = new byte[] { 7, 8, 9 };

            // Body query — returns raw bytes
            var bodyHandler = Substitute.For<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>();
            bodyHandler.HandleAsync(Arg.Any<GetDashboardMessageBodyQuery>())
                .Returns(Task.FromResult(new DashboardMessageBody { Body = bodyBytes, Headers = headerBytes }));
            container.GetInstance<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>().Returns(bodyHandler);

            // Serialization: decode headers + re-encode body
            var internalSerializer = Substitute.For<IInternalSerializer>();
            internalSerializer.ConvertBytesTo<IDictionary<string, object>>(headerBytes).Returns(headers);
            internalSerializer.ConvertToBytes<IDictionary<string, object>>(Arg.Any<IDictionary<string, object>>())
                .Returns(newHeaderBytes);

            var encResult = new MessageInterceptorsResult { Output = new byte[] { 10, 11, 12 } };
            var serializer = Substitute.For<ASerializer>(Substitute.For<IMessageInterceptorRegistrar>());
            serializer.MessageToBytes(Arg.Any<MessageBody>(), Arg.Any<IDictionary<string, object>>())
                .Returns(encResult);

            var compositeSerialization = Substitute.For<ICompositeSerialization>();
            compositeSerialization.InternalSerializer.Returns(internalSerializer);
            compositeSerialization.Serializer.Returns(serializer);
            container.GetInstance<ICompositeSerialization>().Returns(compositeSerialization);

            // Standard headers (interceptor graph key lookup)
            var standardHeaders = Substitute.For<IHeaders>();
            var graphHeaderData = Substitute.For<IMessageContextData<MessageInterceptorsGraph>>();
            graphHeaderData.Name.Returns("Queue-MessageInterceptorGraph");
            standardHeaders.StandardHeaders.MessageInterceptorGraph.Returns(graphHeaderData);
            container.GetInstance<IHeaders>().Returns(standardHeaders);

            // Detail query — for status check
            var detailHandler = Substitute.For<IQueryHandlerAsync<GetDashboardMessageDetailQuery, DashboardMessage>>();
            detailHandler.HandleAsync(Arg.Any<GetDashboardMessageDetailQuery>())
                .Returns(Task.FromResult(new DashboardMessage { Status = messageStatus }));
            container.GetInstance<IQueryHandlerAsync<GetDashboardMessageDetailQuery, DashboardMessage>>().Returns(detailHandler);

            // Update command handler
            var updateHandler = Substitute.For<ICommandHandlerWithOutput<DashboardUpdateMessageBodyCommand, long>>();
            updateHandler.Handle(Arg.Any<DashboardUpdateMessageBodyCommand>()).Returns(1L);
            container.GetInstance<ICommandHandlerWithOutput<DashboardUpdateMessageBodyCommand, long>>().Returns(updateHandler);

            return container;
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
