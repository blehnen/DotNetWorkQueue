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
using System.Threading.Tasks;
using Bunit;
using Bunit.TestDoubles;
using DotNetWorkQueue.Dashboard.Ui.Components.Pages;
using DotNetWorkQueue.Dashboard.Ui.Models;
using DotNetWorkQueue.Dashboard.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Components.Pages
{
    [TestClass]
    public class ConnectionDetailTests : BunitTestBase
    {
        private const string SourceSlug = "acme";
        private static readonly Guid ConnectionId = Guid.NewGuid();
        private static readonly Guid QueueId = Guid.NewGuid();

        [TestMethod]
        public void ShowsSourceNotFound_WhenSlugDoesNotResolve()
        {
            var api = CreateApi();

            var cut = RenderPage(api, sourceNotFound: true);

            StringAssert.Contains(cut.Markup, "not found");
        }

        [TestMethod]
        public void RendersQueueRows_WhenQueuesReturned()
        {
            var api = CreateApi(queues: new List<QueueInfoResponse>
            {
                new() { Id = QueueId, QueueName = "OrdersQueue" }
            });

            var cut = RenderPage(api);

            StringAssert.Contains(cut.Markup, "OrdersQueue");
        }

        [TestMethod]
        public void ShowsNoQueuesMessage_WhenQueueListEmpty()
        {
            var api = CreateApi();

            var cut = RenderPage(api);

            StringAssert.Contains(cut.Markup, "No queues found for this connection.");
        }

        [TestMethod]
        public void ShowsNoJobsMessage_WhenJobListEmpty()
        {
            var api = CreateApi();

            var cut = RenderPage(api);

            StringAssert.Contains(cut.Markup, "No scheduled jobs found for this connection.");
        }

        [TestMethod]
        public void RendersJobRows_WithFormattedTimes()
        {
            var eventTime = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
            var api = CreateApi(jobs: new List<JobResponse>
            {
                new() { JobName = "NightlyRollup", JobEventTime = eventTime, JobScheduledTime = eventTime }
            });

            var cut = RenderPage(api);

            StringAssert.Contains(cut.Markup, "NightlyRollup");
            StringAssert.Contains(cut.Markup, eventTime.LocalDateTime.ToString("G"));
        }

        [TestMethod]
        public void RendersDashes_WhenJobTimesAreNull()
        {
            var api = CreateApi(jobs: new List<JobResponse>
            {
                new() { JobName = "NeverRan" }
            });

            var cut = RenderPage(api);

            StringAssert.Contains(cut.Markup, "NeverRan");
            StringAssert.Contains(cut.Markup, "<td>-</td>");
        }

        [TestMethod]
        public void RendersConsumerCountChip_WhenQueueHasConsumers()
        {
            var api = CreateApi(
                queues: new List<QueueInfoResponse> { new() { Id = QueueId, QueueName = "Busy" } },
                consumerCounts: new Dictionary<Guid, int> { [QueueId] = 3 });

            var cut = RenderPage(api);

            StringAssert.Contains(cut.Markup, "mud-chip");
            StringAssert.Contains(cut.Markup, "3");
        }

        [TestMethod]
        public void RendersZero_WhenQueueHasNoConsumers()
        {
            var api = CreateApi(
                queues: new List<QueueInfoResponse> { new() { Id = QueueId, QueueName = "Idle" } },
                consumerCounts: new Dictionary<Guid, int>());

            var cut = RenderPage(api);

            Assert.DoesNotContain("mud-chip", cut.Markup);
            StringAssert.Contains(cut.Markup, "0");
        }

        [TestMethod]
        public void ShowsErrorAlert_WhenLoadThrows()
        {
            var api = CreateApi();
            api.GetQueuesAsync(Arg.Any<Guid>())
                .Returns<Task<List<QueueInfoResponse>>>(_ => throw new InvalidOperationException("boom"));

            var cut = RenderPage(api);

            StringAssert.Contains(cut.Markup, "boom");
        }

        [TestMethod]
        public void ClickingRetry_AfterFailure_ReloadsAndRendersQueues()
        {
            var callCount = 0;
            var api = CreateApi();
            api.GetQueuesAsync(Arg.Any<Guid>()).Returns<Task<List<QueueInfoResponse>>>(_ =>
            {
                callCount++;
                if (callCount == 1) throw new InvalidOperationException("first load boom");
                return Task.FromResult(new List<QueueInfoResponse>
                {
                    new() { Id = QueueId, QueueName = "RecoveredQueue" }
                });
            });

            var cut = RenderPage(api);
            StringAssert.Contains(cut.Markup, "first load boom");

            cut.Find("button").Click();

            Assert.DoesNotContain("first load boom", cut.Markup);
            StringAssert.Contains(cut.Markup, "RecoveredQueue");
        }

        [TestMethod]
        public void NavigatesToQueue_WithConnectionContext_WhenQueueRowClicked()
        {
            var api = CreateApi(
                queues: new List<QueueInfoResponse> { new() { Id = QueueId, QueueName = "Orders Queue" } },
                connections: new List<ConnectionResponse>
                {
                    new() { Id = ConnectionId, DisplayName = "Prod Conn" }
                });
            var cut = RenderPage(api);
            cut.FindAll("tbody tr")[0].Click();

            var nav = Services.GetRequiredService<BunitNavigationManager>();
            StringAssert.Contains(nav.Uri, $"/source/{SourceSlug}/queues/{QueueId}");
            StringAssert.Contains(nav.Uri, "conn=Prod%20Conn");
            StringAssert.Contains(nav.Uri, $"connId={ConnectionId}");
            StringAssert.Contains(nav.Uri, "queue=Orders%20Queue");
        }

        [TestMethod]
        public void NavigatesWithFallbackNames_WhenConnectionAndQueueNamesMissing()
        {
            var api = CreateApi(queues: new List<QueueInfoResponse> { new() { Id = QueueId } });

            var cut = RenderPage(api);
            cut.FindAll("tbody tr")[0].Click();

            var nav = Services.GetRequiredService<BunitNavigationManager>();
            StringAssert.Contains(nav.Uri, "conn=Connection");
            StringAssert.Contains(nav.Uri, "queue=Queue");
        }

        [TestMethod]
        public void ShowsSourceCrumb_WhenMultipleSourcesConfigured()
        {
            var api = CreateApi(connections: new List<ConnectionResponse>
            {
                new() { Id = ConnectionId, DisplayName = "Prod Conn" }
            });

            var cut = RenderPage(api, extraSource: new DashboardApiSourceConfig { Name = "Other", BaseUrl = "http://other" });

            StringAssert.Contains(cut.Markup, "Acme");
            StringAssert.Contains(cut.Markup, "Prod Conn");
        }

        [TestMethod]
        public void OmitsSourceCrumb_WhenSingleSourceConfigured()
        {
            var api = CreateApi();

            var cut = RenderPage(api);

            StringAssert.Contains(cut.Markup, "Connections");
            Assert.DoesNotContain("Acme", cut.Markup);
        }

        [TestMethod]
        public void SkipsReload_WhenSameSlugAndConnectionRenderedAgain()
        {
            var api = CreateApi();

            var cut = RenderPage(api);
            cut.Render(ps => ps
                .Add(p => p.SourceSlug, SourceSlug)
                .Add(p => p.ConnectionId, ConnectionId));

            api.Received(1).GetQueuesAsync(ConnectionId);
        }

        [TestMethod]
        public void Reloads_WhenConnectionIdChanges()
        {
            var api = CreateApi();
            var otherConnection = Guid.NewGuid();

            var cut = RenderPage(api);
            cut.Render(ps => ps
                .Add(p => p.SourceSlug, SourceSlug)
                .Add(p => p.ConnectionId, otherConnection));

            api.Received(1).GetQueuesAsync(ConnectionId);
            api.Received(1).GetQueuesAsync(otherConnection);
        }

        private static IDashboardApiClient CreateApi(
            List<QueueInfoResponse>? queues = null,
            List<JobResponse>? jobs = null,
            List<ConnectionResponse>? connections = null,
            Dictionary<Guid, int>? consumerCounts = null)
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetConnectionsAsync().Returns(connections ?? new List<ConnectionResponse>());
            api.GetQueuesAsync(Arg.Any<Guid>()).Returns(queues ?? new List<QueueInfoResponse>());
            api.GetJobsAsync(Arg.Any<Guid>()).Returns(jobs ?? new List<JobResponse>());
            api.GetConsumerCountsAsync().Returns(consumerCounts ?? new Dictionary<Guid, int>());
            return api;
        }

        private IRenderedComponent<ConnectionDetail> RenderPage(
            IDashboardApiClient api,
            bool sourceNotFound = false,
            DashboardApiSourceConfig? extraSource = null)
        {
            var multiSource = Substitute.For<IMultiSourceDashboardApiClient>();
            if (sourceNotFound)
            {
                multiSource.GetClientForSource(Arg.Any<string>())
                    .Returns<IDashboardApiClient>(_ => throw new KeyNotFoundException());
            }
            else
            {
                multiSource.GetClientForSource(SourceSlug).Returns(api);
            }

            var sourceConfig = new DashboardApiSourceConfig { Name = "Acme", BaseUrl = "http://acme.example" };
            var sources = new List<DashboardApiSourceConfig> { sourceConfig };
            if (extraSource != null) sources.Add(extraSource);

            var sourceRegistry = Substitute.For<ISourceRegistry>();
            sourceRegistry.GetAll().Returns(sources);
            sourceRegistry.GetBySlug(SourceSlug).Returns(sourceConfig);

            Services.AddSingleton(multiSource);
            Services.AddSingleton(sourceRegistry);

            return Render<ConnectionDetail>(ps => ps
                .Add(p => p.SourceSlug, SourceSlug)
                .Add(p => p.ConnectionId, ConnectionId));
        }
    }
}
