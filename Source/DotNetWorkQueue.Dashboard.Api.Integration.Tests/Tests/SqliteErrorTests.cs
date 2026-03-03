// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Transport.SQLite.Basic;
using FluentAssertions;
using Xunit;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    public class SqliteErrorTests : IAsyncLifetime
    {
        private DashboardTestServer _server;
        private TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation> _fixture;
        private ConsumerStateHelper<SqLiteMessageQueueInit> _consumerHelper;
        private Guid _queueId;

        public async Task InitializeAsync()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            _fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                });

            _fixture.SendMessages<FakeMessage>(2);

            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await _server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await _server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            _queueId = queues[0].Id;

            var errorCountdown = new CountdownEvent(2);
            _consumerHelper = new ConsumerStateHelper<SqLiteMessageQueueInit>();
            _consumerHelper.StartErrorConsumer(_fixture.QueueConnection, _fixture.Scope, errorCountdown);
            errorCountdown.Wait(TimeSpan.FromSeconds(30));

            // Poll until errors are visible via the dashboard API
            await DashboardPollingHelper.WaitForErrorsAsync(_server.Client, _queueId, 2);
        }

        public async Task DisposeAsync()
        {
            _consumerHelper?.Dispose();
            if (_server != null) await _server.DisposeAsync();
            _fixture?.Dispose();
        }

        [Fact]
        public async Task Status_WithErrors()
        {
            var status = await _server.Client.GetFromJsonAsync<QueueStatusResponse>(
                $"api/v1/dashboard/queues/{_queueId}/status");
            status.Error.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Errors_AfterFailure()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/errors");
            paged.Items.Should().NotBeEmpty();
            paged.Items[0].LastException.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DeleteAllErrors_AfterFailure()
        {
            var response = await _server.Client.DeleteAsync(
                $"api/v1/dashboard/queues/{_queueId}/errors");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<DeleteAllResponse>();
            result.Deleted.Should().BeGreaterThan(0);

            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/errors");
            paged.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task RequeueError_Success()
        {
            var paged = await _server.Client.GetFromJsonAsync<PagedResponse<ErrorMessageResponse>>(
                $"api/v1/dashboard/queues/{_queueId}/errors");
            paged.Items.Should().NotBeEmpty();
            var messageId = paged.Items[0].QueueId;

            var response = await _server.Client.PostAsync(
                $"api/v1/dashboard/queues/{_queueId}/messages/{messageId}/requeue", null);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}
