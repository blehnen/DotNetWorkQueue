// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.Memory.Basic;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests.Dashboard
{
    [TestClass]
    public class DashboardQueries
    {
        #region Helpers

        private static void RunDashboardTest(int messageCount, Action<IContainer> testAction)
        {
            RunDashboardTest(messageCount, 0, testAction);
        }

        private static void RunDashboardTest(int messageCount, int consumeCount,
            Action<IContainer> testAction)
        {
            using (var connectionInfo = new IntegrationConnectionInfo())
            {
                var queueName = GenerateQueueName.Create();
                var connection = new QueueConnection(queueName, connectionInfo.ConnectionString);

                using (var queueCreator = new QueueCreationContainer<MemoryDashboardInit>())
                {
                    using (var oCreation =
                           queueCreator.GetQueueCreation<MessageQueueCreation>(connection))
                    {
                        var createResult = oCreation.CreateQueue();
                        Assert.IsTrue(createResult.Success, createResult.ErrorMessage);
                        var scope = oCreation.Scope;

                        try
                        {
                            using (var creator = new QueueContainer<MemoryDashboardInit>(
                                       serviceRegister =>
                                           serviceRegister.RegisterNonScopedSingleton(scope)))
                            {
                                // Send messages — all start in waiting state
                                if (messageCount > 0)
                                {
                                    using (var producer =
                                           creator.CreateProducer<FakeMessage>(connection))
                                    {
                                        for (var i = 0; i < messageCount; i++)
                                        {
                                            producer.Send(new FakeMessage());
                                        }
                                    }
                                }

                                // Move messages from waiting to processing via GetNextMessage
                                if (consumeCount > 0)
                                {
                                    var realScope = (CreationScope)scope;
                                    realScope.ContainedClears.TryPeek(out var obj);
                                    var dataStorage = (IDataStorage)obj;
                                    for (var i = 0; i < consumeCount; i++)
                                    {
                                        dataStorage.GetNextMessage(null,
                                            TimeSpan.FromSeconds(1));
                                    }
                                }

                                // Create admin container and run test action
                                using (var adminContainer =
                                       creator.CreateAdminContainer(connection))
                                {
                                    testAction(adminContainer);
                                }
                            }
                        }
                        finally
                        {
                            oCreation.RemoveQueue();
                            scope.Dispose();
                        }
                    }
                }
            }
        }

        #endregion

        #region Status & Counts

        [TestMethod]
        public void StatusCounts_AllWaiting()
        {
            RunDashboardTest(5, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardStatusCountsQuery,
                            DashboardStatusCounts>>();
                var result = handler.HandleAsync(new GetDashboardStatusCountsQuery())
                    .GetAwaiter().GetResult();

                result.Waiting.Should().Be(5);
                result.Processing.Should().Be(0);
                result.Error.Should().Be(0);
                result.Total.Should().Be(5);
            });
        }

        [TestMethod]
        public void StatusCounts_WithProcessing()
        {
            RunDashboardTest(5, 2, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardStatusCountsQuery,
                            DashboardStatusCounts>>();
                var result = handler.HandleAsync(new GetDashboardStatusCountsQuery())
                    .GetAwaiter().GetResult();

                result.Waiting.Should().Be(3);
                result.Processing.Should().Be(2);
                result.Error.Should().Be(0);
                result.Total.Should().Be(5);
            });
        }

        [TestMethod]
        public void MessageCount_NoFilter()
        {
            RunDashboardTest(5, container =>
            {
                var handler =
                    container.GetInstance<IQueryHandlerAsync<GetDashboardMessageCountQuery, long>>();
                var result = handler.HandleAsync(new GetDashboardMessageCountQuery(null))
                    .GetAwaiter().GetResult();

                result.Should().Be(5);
            });
        }

        [TestMethod]
        public void MessageCount_WaitingFilter()
        {
            RunDashboardTest(5, 2, container =>
            {
                var handler =
                    container.GetInstance<IQueryHandlerAsync<GetDashboardMessageCountQuery, long>>();
                var result = handler.HandleAsync(new GetDashboardMessageCountQuery(0))
                    .GetAwaiter().GetResult();

                result.Should().Be(3);
            });
        }

        [TestMethod]
        public void MessageCount_ProcessingFilter()
        {
            RunDashboardTest(5, 2, container =>
            {
                var handler =
                    container.GetInstance<IQueryHandlerAsync<GetDashboardMessageCountQuery, long>>();
                var result = handler.HandleAsync(new GetDashboardMessageCountQuery(1))
                    .GetAwaiter().GetResult();

                result.Should().Be(2);
            });
        }

        [TestMethod]
        public void MessageCount_ErrorFilter()
        {
            RunDashboardTest(5, container =>
            {
                var handler =
                    container.GetInstance<IQueryHandlerAsync<GetDashboardMessageCountQuery, long>>();
                var result = handler.HandleAsync(new GetDashboardMessageCountQuery(2))
                    .GetAwaiter().GetResult();

                result.Should().Be(0);
            });
        }

        #endregion

        #region Message Listing

        [TestMethod]
        public void Messages_NoFilter()
        {
            RunDashboardTest(3, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessagesQuery,
                            IReadOnlyList<DashboardMessage>>>();
                var result = handler.HandleAsync(new GetDashboardMessagesQuery(0, 100, null))
                    .GetAwaiter().GetResult();

                result.Should().HaveCount(3);
                foreach (var msg in result)
                {
                    msg.QueueId.Should().NotBeNullOrEmpty();
                    msg.Status.Should().Be(0);
                }
            });
        }

        [TestMethod]
        public void Messages_WaitingFilter()
        {
            RunDashboardTest(3, 1, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessagesQuery,
                            IReadOnlyList<DashboardMessage>>>();
                var result = handler.HandleAsync(new GetDashboardMessagesQuery(0, 100, 0))
                    .GetAwaiter().GetResult();

                result.Should().HaveCount(2);
                foreach (var msg in result)
                {
                    msg.Status.Should().Be(0);
                }
            });
        }

        [TestMethod]
        public void Messages_ProcessingFilter()
        {
            RunDashboardTest(3, 1, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessagesQuery,
                            IReadOnlyList<DashboardMessage>>>();
                var result = handler.HandleAsync(new GetDashboardMessagesQuery(0, 100, 1))
                    .GetAwaiter().GetResult();

                result.Should().HaveCount(1);
                result[0].Status.Should().Be(1);
            });
        }

        [TestMethod]
        public void Messages_ErrorFilter_Empty()
        {
            RunDashboardTest(3, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessagesQuery,
                            IReadOnlyList<DashboardMessage>>>();
                var result = handler.HandleAsync(new GetDashboardMessagesQuery(0, 100, 2))
                    .GetAwaiter().GetResult();

                result.Should().BeEmpty();
            });
        }

        #endregion

        #region Message Detail, Body, Headers

        [TestMethod]
        public void MessageDetail_Exists()
        {
            RunDashboardTest(1, container =>
            {
                // Get the message list first to obtain the QueueId
                var listHandler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessagesQuery,
                            IReadOnlyList<DashboardMessage>>>();
                var messages = listHandler
                    .HandleAsync(new GetDashboardMessagesQuery(0, 100, null))
                    .GetAwaiter().GetResult();
                messages.Should().HaveCount(1);

                var detailHandler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessageDetailQuery,
                            DashboardMessage>>();
                var detail = detailHandler
                    .HandleAsync(new GetDashboardMessageDetailQuery(messages[0].QueueId))
                    .GetAwaiter().GetResult();

                detail.Should().NotBeNull();
                detail.QueueId.Should().Be(messages[0].QueueId);
                detail.Status.Should().Be(0);
                detail.QueuedDateTime.Should().NotBeNull();
                detail.CorrelationId.Should().NotBeNullOrEmpty();
            });
        }

        [TestMethod]
        public void MessageDetail_NotFound()
        {
            RunDashboardTest(0, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessageDetailQuery,
                            DashboardMessage>>();
                var result = handler
                    .HandleAsync(new GetDashboardMessageDetailQuery(Guid.NewGuid().ToString()))
                    .GetAwaiter().GetResult();

                result.Should().BeNull();
            });
        }

        [TestMethod]
        public void MessageBody_HasBytes()
        {
            RunDashboardTest(1, container =>
            {
                var listHandler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessagesQuery,
                            IReadOnlyList<DashboardMessage>>>();
                var messages = listHandler
                    .HandleAsync(new GetDashboardMessagesQuery(0, 100, null))
                    .GetAwaiter().GetResult();
                messages.Should().HaveCount(1);

                var bodyHandler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessageBodyQuery,
                            DashboardMessageBody>>();
                var body = bodyHandler
                    .HandleAsync(new GetDashboardMessageBodyQuery(messages[0].QueueId))
                    .GetAwaiter().GetResult();

                body.Should().NotBeNull();
                body.Body.Should().NotBeNullOrEmpty();
                body.Headers.Should().NotBeNullOrEmpty();
            });
        }

        [TestMethod]
        public void MessageHeaders_HasBytes()
        {
            RunDashboardTest(1, container =>
            {
                var listHandler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessagesQuery,
                            IReadOnlyList<DashboardMessage>>>();
                var messages = listHandler
                    .HandleAsync(new GetDashboardMessagesQuery(0, 100, null))
                    .GetAwaiter().GetResult();
                messages.Should().HaveCount(1);

                var headerHandler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessageHeadersQuery,
                            DashboardMessageHeaders>>();
                var headers = headerHandler
                    .HandleAsync(new GetDashboardMessageHeadersQuery(messages[0].QueueId))
                    .GetAwaiter().GetResult();

                headers.Should().NotBeNull();
                headers.Headers.Should().NotBeNullOrEmpty();
            });
        }

        #endregion

        #region Jobs

        [TestMethod]
        public void Jobs_WhenEmpty()
        {
            RunDashboardTest(0, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardJobsQuery,
                            IReadOnlyList<DashboardJob>>>();
                var result = handler.HandleAsync(new GetDashboardJobsQuery())
                    .GetAwaiter().GetResult();

                result.Should().BeEmpty();
            });
        }

        #endregion

        #region Commands

        [TestMethod]
        public void DeleteMessage_Exists()
        {
            RunDashboardTest(1, container =>
            {
                // Get QueueId of the waiting message
                var listHandler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessagesQuery,
                            IReadOnlyList<DashboardMessage>>>();
                var messages = listHandler
                    .HandleAsync(new GetDashboardMessagesQuery(0, 100, null))
                    .GetAwaiter().GetResult();
                messages.Should().HaveCount(1);

                var deleteHandler =
                    container
                        .GetInstance<ICommandHandlerWithOutput<DashboardDeleteMessageCommand,
                            long>>();
                var deleted = deleteHandler.Handle(
                    new DashboardDeleteMessageCommand(messages[0].QueueId));

                deleted.Should().Be(1);

                // Verify message is gone
                var countHandler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardStatusCountsQuery,
                            DashboardStatusCounts>>();
                var counts = countHandler.HandleAsync(new GetDashboardStatusCountsQuery())
                    .GetAwaiter().GetResult();
                counts.Waiting.Should().Be(0);
                counts.Total.Should().Be(0);
            });
        }

        [TestMethod]
        public void DeleteMessage_NotFound()
        {
            RunDashboardTest(0, container =>
            {
                var handler =
                    container
                        .GetInstance<ICommandHandlerWithOutput<DashboardDeleteMessageCommand,
                            long>>();
                var result = handler.Handle(
                    new DashboardDeleteMessageCommand(Guid.NewGuid().ToString()));

                result.Should().Be(0);
            });
        }

        [TestMethod]
        public void DeleteMessage_Processing()
        {
            RunDashboardTest(2, 1, container =>
            {
                // Get the processing message
                var listHandler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardMessagesQuery,
                            IReadOnlyList<DashboardMessage>>>();
                var processing = listHandler
                    .HandleAsync(new GetDashboardMessagesQuery(0, 100, 1))
                    .GetAwaiter().GetResult();
                processing.Should().HaveCount(1);

                var deleteHandler =
                    container
                        .GetInstance<ICommandHandlerWithOutput<DashboardDeleteMessageCommand,
                            long>>();
                var deleted = deleteHandler.Handle(
                    new DashboardDeleteMessageCommand(processing[0].QueueId));

                deleted.Should().Be(1);

                // Verify processing count is now 0
                var countHandler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardStatusCountsQuery,
                            DashboardStatusCounts>>();
                var counts = countHandler.HandleAsync(new GetDashboardStatusCountsQuery())
                    .GetAwaiter().GetResult();
                counts.Processing.Should().Be(0);
                counts.Waiting.Should().Be(1);
                counts.Total.Should().Be(1);
            });
        }

        #endregion

        #region No-op Handlers

        [TestMethod]
        public void StaleMessages_Empty()
        {
            RunDashboardTest(0, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardStaleMessagesQuery,
                            IReadOnlyList<DashboardMessage>>>();
                var result = handler.HandleAsync(new GetDashboardStaleMessagesQuery(60, 0, 100))
                    .GetAwaiter().GetResult();

                result.Should().BeEmpty();
            });
        }

        [TestMethod]
        public void ErrorMessages_Empty()
        {
            RunDashboardTest(0, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardErrorMessagesQuery,
                            IReadOnlyList<DashboardErrorMessage>>>();
                var result = handler.HandleAsync(new GetDashboardErrorMessagesQuery(0, 100))
                    .GetAwaiter().GetResult();

                result.Should().BeEmpty();
            });
        }

        [TestMethod]
        public void ErrorMessageCount_Zero()
        {
            RunDashboardTest(0, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardErrorMessageCountQuery, long>>();
                var result = handler.HandleAsync(new GetDashboardErrorMessageCountQuery())
                    .GetAwaiter().GetResult();

                result.Should().Be(0);
            });
        }

        [TestMethod]
        public void ErrorRetries_Empty()
        {
            RunDashboardTest(0, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardErrorRetriesQuery,
                            IReadOnlyList<DashboardErrorRetry>>>();
                var result = handler
                    .HandleAsync(new GetDashboardErrorRetriesQuery(Guid.NewGuid().ToString()))
                    .GetAwaiter().GetResult();

                result.Should().BeEmpty();
            });
        }

        [TestMethod]
        public void Configuration_Null()
        {
            RunDashboardTest(0, container =>
            {
                var handler =
                    container
                        .GetInstance<IQueryHandlerAsync<GetDashboardConfigurationQuery, byte[]>>();
                var result = handler.HandleAsync(new GetDashboardConfigurationQuery())
                    .GetAwaiter().GetResult();

                result.Should().BeNull();
            });
        }

        [TestMethod]
        public void DeleteAllErrors_Zero()
        {
            RunDashboardTest(0, container =>
            {
                var handler =
                    container
                        .GetInstance<ICommandHandlerWithOutput<
                            DashboardDeleteAllErrorMessagesCommand, long>>();
                var result = handler.Handle(new DashboardDeleteAllErrorMessagesCommand());

                result.Should().Be(0);
            });
        }

        [TestMethod]
        public void RequeueError_Zero()
        {
            RunDashboardTest(0, container =>
            {
                var handler =
                    container
                        .GetInstance<ICommandHandlerWithOutput<
                            DashboardRequeueErrorMessageCommand, long>>();
                var result = handler.Handle(
                    new DashboardRequeueErrorMessageCommand(Guid.NewGuid().ToString()));

                result.Should().Be(0);
            });
        }

        [TestMethod]
        public void ResetStale_Zero()
        {
            RunDashboardTest(0, container =>
            {
                var handler =
                    container
                        .GetInstance<ICommandHandlerWithOutput<
                            DashboardResetStaleMessageCommand, long>>();
                var result = handler.Handle(
                    new DashboardResetStaleMessageCommand(Guid.NewGuid().ToString()));

                result.Should().Be(0);
            });
        }

        [TestMethod]
        public void UpdateBody_Zero()
        {
            RunDashboardTest(0, container =>
            {
                var handler =
                    container
                        .GetInstance<ICommandHandlerWithOutput<
                            DashboardUpdateMessageBodyCommand, long>>();
                var result = handler.Handle(
                    new DashboardUpdateMessageBodyCommand(
                        Guid.NewGuid().ToString(),
                        new byte[] { 1, 2, 3 },
                        new byte[] { 4, 5, 6 }));

                result.Should().Be(0);
            });
        }

        #endregion
    }
}
