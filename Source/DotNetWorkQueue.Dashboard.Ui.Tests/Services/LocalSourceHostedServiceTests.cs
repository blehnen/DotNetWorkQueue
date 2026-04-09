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
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Ui.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Services
{
    [TestClass]
    public class LocalSourceHostedServiceTests
    {
        private static (LocalSourceHostedService service, IServer server, SourceRegistry registry, ILogger<LocalSourceHostedService> logger)
            CreateService(string localBaseUrl = "http://localhost:5000", IServerAddressesFeature? addressesFeature = null)
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                new() { Name = "Local", BaseUrl = localBaseUrl }
            };
            var registry = new SourceRegistry(sources);

            var features = new FeatureCollection();
            if (addressesFeature != null)
            {
                features.Set(addressesFeature);
            }

            var server = Substitute.For<IServer>();
            server.Features.Returns(features);

            var logger = Substitute.For<ILogger<LocalSourceHostedService>>();
            var service = new LocalSourceHostedService(server, registry, logger);

            return (service, server, registry, logger);
        }

        [TestMethod]
        public async Task StartAsync_Updates_BaseUrl_When_ServerAddressesFeature_Available()
        {
            var addressesFeature = Substitute.For<IServerAddressesFeature>();
            addressesFeature.Addresses.Returns(new List<string> { "http://localhost:5123" });

            var (service, _, registry, _) = CreateService(addressesFeature: addressesFeature);

            await service.StartAsync(CancellationToken.None);

            registry.GetByName("Local")!.BaseUrl.Should().Be("http://localhost:5123");
        }

        [TestMethod]
        public async Task StartAsync_Leaves_BaseUrl_When_No_ServerAddressesFeature()
        {
            var (service, _, registry, _) = CreateService(addressesFeature: null);

            await service.StartAsync(CancellationToken.None);

            registry.GetByName("Local")!.BaseUrl.Should().Be("http://localhost:5000");
        }

        [TestMethod]
        public async Task StartAsync_Leaves_BaseUrl_When_No_Addresses()
        {
            var addressesFeature = Substitute.For<IServerAddressesFeature>();
            addressesFeature.Addresses.Returns(new List<string>());

            var (service, _, registry, _) = CreateService(addressesFeature: addressesFeature);

            await service.StartAsync(CancellationToken.None);

            registry.GetByName("Local")!.BaseUrl.Should().Be("http://localhost:5000");
        }

        [TestMethod]
        public async Task StartAsync_Logs_Warning_When_Cannot_Resolve()
        {
            var (service, _, _, logger) = CreateService(addressesFeature: null);

            await service.StartAsync(CancellationToken.None);

            logger.Received(1).Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>());
        }

        [TestMethod]
        public async Task StopAsync_Returns_Completed_Task()
        {
            var (service, _, _, _) = CreateService();

            var task = service.StopAsync(CancellationToken.None);

            task.IsCompleted.Should().BeTrue();
            await task;
        }
    }
}
