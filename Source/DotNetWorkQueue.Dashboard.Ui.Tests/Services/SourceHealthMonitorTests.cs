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

#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Ui.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Services
{
    [TestClass]
    public class SourceHealthMonitorTests
    {
        private static DashboardApiSourceConfig CreateSource(string name, string baseUrl = "https://example.com")
        {
            return new DashboardApiSourceConfig { Name = name, BaseUrl = baseUrl };
        }

        private static (SourceHealthMonitor monitor, IMultiSourceDashboardApiClient multiClient, ISourceRegistry registry, ILogger<SourceHealthMonitor> logger) CreateSut(
            params DashboardApiSourceConfig[] sources)
        {
            var registry = new SourceRegistry(new List<DashboardApiSourceConfig>(sources));
            var multiClient = Substitute.For<IMultiSourceDashboardApiClient>();
            var logger = Substitute.For<ILogger<SourceHealthMonitor>>();

            var monitor = new SourceHealthMonitor(multiClient, registry, logger);
            return (monitor, multiClient, registry, logger);
        }

        [TestMethod]
        public void GetHealth_Returns_Unknown_For_Unpolled_Source()
        {
            var (sut, _, _, _) = CreateSut(CreateSource("Local"));

            var result = sut.GetHealth("local");

            result.Should().NotBeNull();
            result.Status.Should().Be(SourceHealthStatus.Unknown);
        }

        [TestMethod]
        public void GetAllHealth_Returns_Empty_Before_Polling()
        {
            var (sut, _, _, _) = CreateSut(CreateSource("Local"));

            var result = sut.GetAllHealth();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task PollAsync_Sets_Healthy_When_GetSettingsAsync_Succeeds()
        {
            var source = CreateSource("Local", "http://localhost:5000");
            var (sut, multiClient, _, _) = CreateSut(source);

            var apiClient = Substitute.For<IDashboardApiClient>();
            apiClient.GetSettingsAsync().Returns(Task.FromResult<Models.DashboardSettingsResponse?>(new Models.DashboardSettingsResponse()));
            multiClient.GetClientForSource(source.Slug).Returns(apiClient);

            var before = DateTimeOffset.UtcNow;
            await sut.PollAllSourcesAsync(CancellationToken.None);

            var result = sut.GetHealth(source.Slug);
            result.Status.Should().Be(SourceHealthStatus.Healthy);
            result.LastChecked.Should().BeOnOrAfter(before);
            result.ErrorMessage.Should().BeNull();
        }

        [TestMethod]
        public async Task PollAsync_Sets_Unhealthy_When_GetSettingsAsync_Throws()
        {
            var source = CreateSource("Local", "http://localhost:5000");
            var (sut, multiClient, _, _) = CreateSut(source);

            var apiClient = Substitute.For<IDashboardApiClient>();
            apiClient.GetSettingsAsync().Returns<Models.DashboardSettingsResponse?>(_ => throw new HttpRequestException("Connection refused"));
            multiClient.GetClientForSource(source.Slug).Returns(apiClient);

            await sut.PollAllSourcesAsync(CancellationToken.None);

            var result = sut.GetHealth(source.Slug);
            result.Status.Should().Be(SourceHealthStatus.Unhealthy);
            result.ErrorMessage.Should().Contain("Connection refused");
        }

        [TestMethod]
        public async Task PollAsync_Transitions_Healthy_To_Unhealthy()
        {
            var source = CreateSource("Local", "http://localhost:5000");
            var (sut, multiClient, _, _) = CreateSut(source);

            var apiClient = Substitute.For<IDashboardApiClient>();
            multiClient.GetClientForSource(source.Slug).Returns(apiClient);

            // First poll: healthy
            apiClient.GetSettingsAsync().Returns(Task.FromResult<Models.DashboardSettingsResponse?>(new Models.DashboardSettingsResponse()));
            await sut.PollAllSourcesAsync(CancellationToken.None);
            sut.GetHealth(source.Slug).Status.Should().Be(SourceHealthStatus.Healthy);

            // Second poll: unhealthy
            apiClient.GetSettingsAsync().Returns<Models.DashboardSettingsResponse?>(_ => throw new HttpRequestException("Timeout"));
            await sut.PollAllSourcesAsync(CancellationToken.None);
            sut.GetHealth(source.Slug).Status.Should().Be(SourceHealthStatus.Unhealthy);
            sut.GetHealth(source.Slug).ErrorMessage.Should().Contain("Timeout");
        }

        [TestMethod]
        public async Task PollAsync_Transitions_Unhealthy_To_Healthy()
        {
            var source = CreateSource("Local", "http://localhost:5000");
            var (sut, multiClient, _, _) = CreateSut(source);

            var shouldThrow = true;
            var apiClient = Substitute.For<IDashboardApiClient>();
            apiClient.GetSettingsAsync().Returns<Models.DashboardSettingsResponse?>(_ =>
            {
                if (shouldThrow) throw new HttpRequestException("Down");
                return new Models.DashboardSettingsResponse();
            });
            multiClient.GetClientForSource(source.Slug).Returns(apiClient);

            // First poll: unhealthy
            await sut.PollAllSourcesAsync(CancellationToken.None);
            sut.GetHealth(source.Slug).Status.Should().Be(SourceHealthStatus.Unhealthy);

            // Second poll: healthy (recovery)
            shouldThrow = false;
            await sut.PollAllSourcesAsync(CancellationToken.None);
            sut.GetHealth(source.Slug).Status.Should().Be(SourceHealthStatus.Healthy);
            sut.GetHealth(source.Slug).ErrorMessage.Should().BeNull();
        }

        [TestMethod]
        public async Task PollAsync_GetAllHealth_Returns_All_Polled_Sources()
        {
            var source1 = CreateSource("Local", "http://localhost:5000");
            var source2 = CreateSource("Production", "https://prod.example.com");
            var (sut, multiClient, _, _) = CreateSut(source1, source2);

            var apiClient1 = Substitute.For<IDashboardApiClient>();
            apiClient1.GetSettingsAsync().Returns(Task.FromResult<Models.DashboardSettingsResponse?>(new Models.DashboardSettingsResponse()));
            multiClient.GetClientForSource(source1.Slug).Returns(apiClient1);

            var apiClient2 = Substitute.For<IDashboardApiClient>();
            apiClient2.GetSettingsAsync().Returns<Models.DashboardSettingsResponse?>(_ => throw new HttpRequestException("Down"));
            multiClient.GetClientForSource(source2.Slug).Returns(apiClient2);

            await sut.PollAllSourcesAsync(CancellationToken.None);

            var all = sut.GetAllHealth();
            all.Should().HaveCount(2);
            all[source1.Slug].Status.Should().Be(SourceHealthStatus.Healthy);
            all[source2.Slug].Status.Should().Be(SourceHealthStatus.Unhealthy);
        }

        [TestMethod]
        public async Task PollAsync_Logs_State_Transitions()
        {
            var source = CreateSource("Local", "http://localhost:5000");
            var (sut, multiClient, _, logger) = CreateSut(source);

            var shouldThrow = false;
            var apiClient = Substitute.For<IDashboardApiClient>();
            apiClient.GetSettingsAsync().Returns<Models.DashboardSettingsResponse?>(_ =>
            {
                if (shouldThrow) throw new HttpRequestException("Connection refused");
                return new Models.DashboardSettingsResponse();
            });
            multiClient.GetClientForSource(source.Slug).Returns(apiClient);

            // First poll: healthy (transition from Unknown -> Healthy, should log)
            await sut.PollAllSourcesAsync(CancellationToken.None);

            // Second poll: still healthy (no transition, should NOT log)
            logger.ClearReceivedCalls();
            await sut.PollAllSourcesAsync(CancellationToken.None);

            // Verify no log call was made for same-state poll
            logger.ReceivedCalls().Should().BeEmpty();

            // Third poll: unhealthy (transition Healthy -> Unhealthy, should log)
            shouldThrow = true;
            await sut.PollAllSourcesAsync(CancellationToken.None);

            // Verify a log call was made for the transition
            logger.ReceivedCalls().Should().NotBeEmpty();

            // Fourth poll: still unhealthy (no transition, should NOT log)
            logger.ClearReceivedCalls();
            await sut.PollAllSourcesAsync(CancellationToken.None);
            logger.ReceivedCalls().Should().BeEmpty();

            // Fifth poll: recovery (Unhealthy -> Healthy, should log)
            shouldThrow = false;
            await sut.PollAllSourcesAsync(CancellationToken.None);
            logger.ReceivedCalls().Should().NotBeEmpty();
        }
    }
}
