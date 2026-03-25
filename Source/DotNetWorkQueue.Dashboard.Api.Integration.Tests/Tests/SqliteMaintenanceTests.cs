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
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Transport.SQLite.Basic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class SqliteMaintenanceTests
    {
        [TestMethod]
        public async Task Maintenance_Disabled_By_Default()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                });

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");

            var status = await server.Client.GetFromJsonAsync<MaintenanceStatusResponse>(
                $"api/v1/dashboard/queues/{queues[0].Id}/maintenance");

            status.HostMaintenance.Should().BeFalse();
            status.IsRunning.Should().BeFalse();
            status.LastRunUtc.Should().BeNull();
        }

        [TestMethod]
        public async Task Maintenance_Enabled_IsRunning_And_LastRun_Populates()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                    options.Options.EnableMessageExpiration = true;
                });

            fixture.SendMessages<FakeMessage>(1);

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName, hostMaintenance: true));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            // Maintenance should be running
            var status = await server.Client.GetFromJsonAsync<MaintenanceStatusResponse>(
                $"api/v1/dashboard/queues/{queueId}/maintenance");

            status.HostMaintenance.Should().BeTrue();
            status.IsRunning.Should().BeTrue();

            // Wait for at least one monitor cycle to complete (monitors run on timer)
            // The heartbeat monitor runs immediately on Start, so LastRunUtc should populate quickly
            MaintenanceStatusResponse polled = null;
            for (var i = 0; i < 30; i++)
            {
                polled = await server.Client.GetFromJsonAsync<MaintenanceStatusResponse>(
                    $"api/v1/dashboard/queues/{queueId}/maintenance");
                if (polled.LastRunUtc.HasValue)
                    break;
                Thread.Sleep(500);
            }

            polled.LastRunUtc.Should().NotBeNull();
            polled.LastRunUtc.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30));
        }

        [TestMethod]
        public async Task Maintenance_Stopped_Consumer_Not_Running()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                });

            // No messages sent, no consumer started — maintenance not hosted
            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");

            var status = await server.Client.GetFromJsonAsync<MaintenanceStatusResponse>(
                $"api/v1/dashboard/queues/{queues[0].Id}/maintenance");

            status.HostMaintenance.Should().BeFalse();
            status.IsRunning.Should().BeFalse();
            status.LastRunUtc.Should().BeNull();
        }

        [TestMethod]
        public async Task Multiple_Queues_Different_Maintenance_Modes()
        {
            var queueName1 = QueueNameGenerator.Create();
            var queueName2 = QueueNameGenerator.Create();
            var connStr1 = ConnectionStrings.CreateSqliteInMemory(queueName1);
            var connStr2 = ConnectionStrings.CreateSqliteInMemory(queueName2);

            using var fixture1 = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName1, connStr1,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                    options.Options.EnableMessageExpiration = true;
                });

            using var fixture2 = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName2, connStr2,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                });

            fixture1.SendMessages<FakeMessage>(1);

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                // Queue 1 has maintenance enabled
                options.AddConnection<SqLiteMessageQueueInit>(connStr1,
                    conn => conn.AddQueue(queueName1, hostMaintenance: true));
                // Queue 2 does NOT have maintenance enabled (different connection)
                options.AddConnection<SqLiteMessageQueueInit>(connStr2,
                    conn => conn.AddQueue(queueName2));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");

            // Find queue IDs across connections
            Guid queueId1 = Guid.Empty, queueId2 = Guid.Empty;
            foreach (var conn in connections)
            {
                var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                    $"api/v1/dashboard/connections/{conn.Id}/queues");
                foreach (var q in queues)
                {
                    if (q.QueueName == queueName1) queueId1 = q.Id;
                    if (q.QueueName == queueName2) queueId2 = q.Id;
                }
            }

            queueId1.Should().NotBeEmpty();
            queueId2.Should().NotBeEmpty();

            // Queue 1: maintenance hosted and running
            var status1 = await server.Client.GetFromJsonAsync<MaintenanceStatusResponse>(
                $"api/v1/dashboard/queues/{queueId1}/maintenance");
            status1.HostMaintenance.Should().BeTrue();
            status1.IsRunning.Should().BeTrue();

            // Queue 2: maintenance NOT hosted
            var status2 = await server.Client.GetFromJsonAsync<MaintenanceStatusResponse>(
                $"api/v1/dashboard/queues/{queueId2}/maintenance");
            status2.HostMaintenance.Should().BeFalse();
            status2.IsRunning.Should().BeFalse();
        }

        [TestMethod]
        public async Task Maintenance_Enabled_Without_Expiration_Still_Runs()
        {
            var queueName = QueueNameGenerator.Create();
            var connStr = ConnectionStrings.CreateSqliteInMemory(queueName);

            using var fixture = new TransportFixture<SqLiteMessageQueueInit, SqLiteMessageQueueCreation>(
                queueName, connStr,
                options =>
                {
                    options.Options.EnableStatus = true;
                    options.Options.EnableStatusTable = true;
                    options.Options.EnableHeartBeat = true;
                    // Note: EnableMessageExpiration is false
                });

            fixture.SendMessages<FakeMessage>(1);

            await using var server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = false;
                options.AddConnection<SqLiteMessageQueueInit>(connStr,
                    conn => conn.AddQueue(queueName, hostMaintenance: true));
            });

            var connections = await server.Client.GetFromJsonAsync<List<ConnectionResponse>>(
                "api/v1/dashboard/connections");
            var queues = await server.Client.GetFromJsonAsync<List<QueueInfoResponse>>(
                $"api/v1/dashboard/connections/{connections[0].Id}/queues");
            var queueId = queues[0].Id;

            var status = await server.Client.GetFromJsonAsync<MaintenanceStatusResponse>(
                $"api/v1/dashboard/queues/{queueId}/maintenance");

            status.HostMaintenance.Should().BeTrue();
            status.IsRunning.Should().BeTrue();

            // Wait for at least one monitor cycle
            MaintenanceStatusResponse polled = null;
            for (var i = 0; i < 30; i++)
            {
                polled = await server.Client.GetFromJsonAsync<MaintenanceStatusResponse>(
                    $"api/v1/dashboard/queues/{queueId}/maintenance");
                if (polled.LastRunUtc.HasValue)
                    break;
                Thread.Sleep(500);
            }

            polled.LastRunUtc.Should().NotBeNull();
        }
    }
}
