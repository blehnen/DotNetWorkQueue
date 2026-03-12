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
    }
}
