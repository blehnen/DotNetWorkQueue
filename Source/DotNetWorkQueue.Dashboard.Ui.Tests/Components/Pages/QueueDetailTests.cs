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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using DotNetWorkQueue.Dashboard.Ui.Components.Pages;
using DotNetWorkQueue.Dashboard.Ui.Components.Shared;
using DotNetWorkQueue.Dashboard.Ui.Models;
using DotNetWorkQueue.Dashboard.Ui.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Components.Pages
{
    [TestClass]
    public class QueueDetailTests : BunitTestBase
    {
        private const string SourceSlug = "acme";
        private static readonly Guid TestQueueId = Guid.NewGuid();

        [TestMethod]
        public void LoadsQueueData_OnInitialRender()
        {
            var api = CreateDefaultApi(waiting: 1, processing: 2, error: 3, total: 6);

            RenderPage(api);

            api.Received(1).GetQueueStatusAsync(TestQueueId);
            api.Received(1).GetQueueFeaturesAsync(TestQueueId);
            api.Received(1).GetStaleMessagesAsync(TestQueueId, 60, 0, 1);
            api.Received(1).GetConsumersAsync(TestQueueId);
            api.Received(1).GetSettingsAsync();
        }

        [TestMethod]
        public void RendersStatusCounts_AfterLoad()
        {
            var api = CreateDefaultApi(waiting: 1, processing: 2, error: 3, total: 6);

            var cut = RenderPage(api);

            StringAssert.Contains(cut.Markup, "Waiting");
            StringAssert.Contains(cut.Markup, "Processing");
            StringAssert.Contains(cut.Markup, "Total");
        }

        [TestMethod]
        public void RendersQueueName_FromQueryString()
        {
            var api = CreateDefaultApi();

            var cut = RenderPage(api, queryString: "?conn=Acme%20Prod&connId=" + Guid.NewGuid() + "&queue=OrdersQueue");

            StringAssert.Contains(cut.Markup, "OrdersQueue");
            StringAssert.Contains(cut.Markup, "Acme Prod");
        }

        [TestMethod]
        public void ResolvesQueueName_ViaConnectionLookup_WhenQueryStringMissing()
        {
            var api = CreateDefaultApi();
            var connectionId = Guid.NewGuid();
            api.GetConnectionsAsync().Returns(new List<ConnectionResponse>
            {
                new() { Id = connectionId, DisplayName = "Looked Up Conn", QueueCount = 1 }
            });
            api.GetQueuesAsync(connectionId).Returns(new List<QueueInfoResponse>
            {
                new() { Id = TestQueueId, QueueName = "LookedUpQueue" }
            });

            var cut = RenderPage(api);

            StringAssert.Contains(cut.Markup, "LookedUpQueue");
            StringAssert.Contains(cut.Markup, "Looked Up Conn");
        }

        [TestMethod]
        public void ShowsSourceError_WhenSourceNotFound()
        {
            var api = CreateDefaultApi();

            var cut = RenderPage(api, sourceNotFound: true);

            StringAssert.Contains(cut.Markup, "not found");
        }

        [TestMethod]
        public void ShowsErrorAlert_WhenLoadThrows()
        {
            var api = CreateDefaultApi();
            api.GetQueueStatusAsync(Arg.Any<Guid>())
                .Returns<Task<QueueStatusResponse?>>(_ => throw new InvalidOperationException("load boom"));

            var cut = RenderPage(api);

            StringAssert.Contains(cut.Markup, "load boom");
            StringAssert.Contains(cut.Markup, "Retry");
        }

        [TestMethod]
        public void RendersWithoutThrowing_WhenApiCallsAreUnconfigured()
        {
            var api = Substitute.For<IDashboardApiClient>();

            var cut = RenderPage(api);

            Assert.IsNotNull(cut.Markup);
        }

        // ---- Sections / tabs ----

        [TestMethod]
        public void RendersFeatureChips_ForEnabledFeatures()
        {
            var api = CreateDefaultApi();
            api.GetQueueFeaturesAsync(Arg.Any<Guid>()).Returns(new QueueFeaturesResponse
            {
                EnablePriority = true,
                EnableHeartBeat = true
            });

            var cut = RenderPage(api);

            StringAssert.Contains(cut.Markup, "Priority");
            StringAssert.Contains(cut.Markup, "HeartBeat");
            Assert.DoesNotContain("Routing", cut.Markup);
        }

        [TestMethod]
        public void OmitsFeatureChips_WhenFeaturesUnavailable()
        {
            var api = CreateDefaultApi();
            api.GetQueueFeaturesAsync(Arg.Any<Guid>()).Returns((QueueFeaturesResponse?)null);

            var cut = RenderPage(api);

            Assert.DoesNotContain("Priority", cut.Markup);
        }

        [TestMethod]
        public void IncludesSourceName_InBreadcrumbs_WhenMultipleSourcesConfigured()
        {
            var api = CreateDefaultApi();

            var cut = RenderPage(api, extraSource: new DashboardApiSourceConfig { Name = "Other", BaseUrl = "http://other.example" });

            StringAssert.Contains(cut.Markup, "Acme");
        }

        // ---- Actions ----

        [TestMethod]
        public void ClickingRefreshButton_ReloadsCountsOnly()
        {
            var api = CreateDefaultApi(waiting: 1, processing: 2, error: 3, total: 6);
            var cut = RenderPage(api);

            cut.Find("button[aria-label='Refresh']").Click();

            api.Received(2).GetQueueStatusAsync(TestQueueId);
            api.Received(2).GetStaleMessagesAsync(TestQueueId, 60, 0, 1);
            api.Received(2).GetConsumersAsync(TestQueueId);
            api.Received(1).GetQueueFeaturesAsync(TestQueueId);
            api.Received(1).GetSettingsAsync();
        }

        [TestMethod]
        public void ClickingRetryButton_AfterLoadFailure_RecoversAndRendersStatus()
        {
            var callCount = 0;
            var api = CreateDefaultApi();
            api.GetQueueStatusAsync(Arg.Any<Guid>()).Returns<Task<QueueStatusResponse?>>(_ =>
            {
                callCount++;
                if (callCount == 1) throw new InvalidOperationException("first load boom");
                return Task.FromResult<QueueStatusResponse?>(new QueueStatusResponse { Waiting = 5 });
            });
            var cut = RenderPage(api);
            StringAssert.Contains(cut.Markup, "first load boom");

            cut.Find("button").Click();

            Assert.DoesNotContain("first load boom", cut.Markup);
            StringAssert.Contains(cut.Markup, "Waiting");
        }

        [TestMethod]
        public void ClickingWaitingCard_FiltersMessagesTab_ToWaitingStatus()
        {
            var api = CreateDefaultApi();
            var cut = RenderPage(api);

            cut.FindAll(StatusCardSelector)[0].Click();

            api.Received().GetMessagesAsync(TestQueueId, 0, 25, 0);
            Assert.AreEqual(0, GetActiveTab(cut));
        }

        [TestMethod]
        public void ClickingErrorCard_SwitchesActiveTabToErrors()
        {
            var api = CreateDefaultApi();
            var cut = RenderPage(api);

            cut.FindAll(StatusCardSelector)[2].Click();

            Assert.AreEqual(1, GetActiveTab(cut));
        }

        [TestMethod]
        public void OpenMessageDrawer_ViaMessagesTabCallback_OpensDrawerWithMessageId()
        {
            var api = CreateDefaultApi();
            var cut = RenderPage(api);

            var messagesTab = cut.FindComponent<MessagesTab>();
            cut.InvokeAsync(() => messagesTab.Instance.OnMessageSelected.InvokeAsync("msg-123"));

            var drawer = cut.FindComponent<MessageDetailDrawer>().Instance;
            Assert.IsTrue(drawer.Open);
            Assert.AreEqual("msg-123", drawer.MessageId);
        }

        [TestMethod]
        public void ClosingDrawer_ViaOpenChangedCallback_ClearsSelectedMessage()
        {
            var api = CreateDefaultApi();
            var cut = RenderPage(api);
            var messagesTab = cut.FindComponent<MessagesTab>();
            cut.InvokeAsync(() => messagesTab.Instance.OnMessageSelected.InvokeAsync("msg-123"));

            var drawer = cut.FindComponent<MessageDetailDrawer>();
            cut.InvokeAsync(() => drawer.Instance.OpenChanged.InvokeAsync(false));

            var updatedDrawer = cut.FindComponent<MessageDetailDrawer>().Instance;
            Assert.IsFalse(updatedDrawer.Open);
            Assert.IsNull(updatedDrawer.MessageId);
        }

        private static IDashboardApiClient CreateDefaultApi(long waiting = 0, long processing = 0, long error = 0, long total = 0)
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetQueueStatusAsync(Arg.Any<Guid>())
                .Returns(new QueueStatusResponse { Waiting = waiting, Processing = processing, Error = error, Total = total });
            api.GetQueueFeaturesAsync(Arg.Any<Guid>()).Returns(new QueueFeaturesResponse());
            api.GetStaleMessagesAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(new PagedResponse<MessageResponse> { Items = new List<MessageResponse>(), TotalCount = 0 });
            api.GetConsumersAsync(Arg.Any<Guid?>()).Returns(new List<ConsumerInfoResponse>());
            api.GetSettingsAsync().Returns(new DashboardSettingsResponse { ReadOnly = false });
            api.GetConnectionsAsync().Returns(new List<ConnectionResponse>());
            return api;
        }

        private IRenderedComponent<IComponent> RenderPage(
            IDashboardApiClient api,
            bool sourceNotFound = false,
            string? queryString = null,
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

            if (queryString != null)
            {
                var nav = Services.GetRequiredService<NavigationManager>();
                nav.NavigateTo($"http://localhost/source/{SourceSlug}/queues/{TestQueueId}{queryString}");
            }

            return RenderWithMudProvider<QueueDetail>(
                (nameof(QueueDetail.SourceSlug), SourceSlug),
                (nameof(QueueDetail.QueueId), TestQueueId));
        }

        /// <summary>
        /// The four clickable status cards. Plain "div.mud-paper" also matches the two
        /// popover containers MudPopoverProvider renders ahead of the page content.
        /// </summary>
        private const string StatusCardSelector = "div.mud-paper.mud-elevation-2";

        private static int GetActiveTab(IRenderedComponent<IComponent> cut) =>
            (int)typeof(QueueDetail)
                .GetField("_activeTab", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(cut.FindComponent<QueueDetail>().Instance)!;
    }
}
