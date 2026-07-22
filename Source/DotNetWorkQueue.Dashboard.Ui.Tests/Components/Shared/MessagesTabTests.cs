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
using DotNetWorkQueue.Dashboard.Ui.Components.Shared;
using DotNetWorkQueue.Dashboard.Ui.Models;
using DotNetWorkQueue.Dashboard.Ui.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MudBlazor;
using NSubstitute;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Components.Shared
{
    [TestClass]
    public class MessagesTabTests : BunitTestBase
    {
        private static readonly Guid TestQueueId = Guid.NewGuid();

        [TestMethod]
        public void LoadsMessages_OnInitialization_WithUnfilteredStatus()
        {
            var api = CreateApiWithMessages();

            RenderMessagesTab(api);

            api.Received(1).GetMessagesAsync(TestQueueId, 0, 25, null);
        }

        [TestMethod]
        public void RendersRows_WhenMessagesPresent()
        {
            var api = CreateApiWithMessages();

            var cut = RenderMessagesTab(api);

            StringAssert.Contains(cut.Markup, "abc-123");
        }

        [TestMethod]
        public void ShowsNoRecordsContent_WhenEmpty()
        {
            var api = CreateApiWithMessages(totalCount: 0, includeItem: false);

            var cut = RenderMessagesTab(api);

            StringAssert.Contains(cut.Markup, "No messages found.");
        }

        [TestMethod]
        public void ShowsErrorAlert_WhenLoadMessagesThrows()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetMessagesAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int?>())
                .Returns<Task<PagedResponse<MessageResponse>>>(_ => throw new InvalidOperationException("boom"));

            var cut = RenderMessagesTab(api);

            StringAssert.Contains(cut.Markup, "boom");
        }

        [TestMethod]
        public async Task ChangingStatusFilter_RequeriesWithSelectedValue()
        {
            var api = CreateApiWithMessages();

            var cut = RenderMessagesTab(api);
            var select = cut.FindComponent<MudSelect<int?>>();
            await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(2));

            await api.Received(1).GetMessagesAsync(TestQueueId, 0, 25, 2);
        }

        [TestMethod]
        public void TruncatesLongQueueId_AndRendersDashWhenMissing()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetMessagesAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int?>())
                .Returns(new PagedResponse<MessageResponse>
                {
                    Items = new List<MessageResponse>
                    {
                        new() { QueueId = "0123456789abcdef", Status = 1, QueuedDateTime = DateTimeOffset.UtcNow },
                        new() { QueueId = null, Status = 3 }
                    },
                    TotalCount = 2
                });

            var cut = RenderMessagesTab(api);

            StringAssert.Contains(cut.Markup, "0123456789ab...");
            StringAssert.Contains(cut.Markup, "Processing");
            StringAssert.Contains(cut.Markup, "Processed");
        }

        [TestMethod]
        public void RendersDelayedChip_ForFutureScheduledWaitingMessage()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetMessagesAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int?>())
                .Returns(new PagedResponse<MessageResponse>
                {
                    Items = new List<MessageResponse>
                    {
                        new()
                        {
                            QueueId = "delayed-1",
                            Status = 0,
                            QueuedDateTime = DateTimeOffset.UtcNow,
                            QueueProcessTime = DateTimeOffset.Now.AddHours(1)
                        }
                    },
                    TotalCount = 1
                });

            var cut = RenderWithMudProvider<MessagesTab>(
                (nameof(MessagesTab.QueueId), TestQueueId),
                (nameof(MessagesTab.Api), api),
                (nameof(MessagesTab.Features), new QueueFeaturesResponse { EnableDelayedProcessing = true }));

            StringAssert.Contains(cut.Markup, "Delayed");
            StringAssert.Contains(cut.Markup, "Scheduled");
        }

        [TestMethod]
        public async Task ChangingPage_RequeriesWithZeroBasedIndex()
        {
            var api = CreateApiWithMessages(totalCount: 60);

            var cut = RenderMessagesTab(api);
            var pagination = cut.FindComponent<MudPagination>();
            await cut.InvokeAsync(() => pagination.Instance.SelectedChanged.InvokeAsync(3));

            await api.Received(1).GetMessagesAsync(TestQueueId, 2, 25, null);
        }

        [TestMethod]
        public async Task RowClick_RaisesOnMessageSelected_WithQueueId()
        {
            var api = CreateApiWithMessages();
            string? selected = null;

            var cut = RenderWithMudProvider<MessagesTab>(
                (nameof(MessagesTab.QueueId), TestQueueId),
                (nameof(MessagesTab.Api), api),
                (nameof(MessagesTab.OnMessageSelected), EventCallback.Factory.Create<string>(this, s => selected = s)));

            var table = cut.FindComponent<MudTable<MessageResponse>>();
            await cut.InvokeAsync(() => table.Instance.OnRowClick.InvokeAsync(
                new TableRowClickEventArgs<MessageResponse>(new MouseEventArgs(), null!, new MessageResponse { QueueId = "abc-123" })));

            Assert.AreEqual("abc-123", selected);
        }

        [TestMethod]
        public async Task RowClick_Ignored_WhenMessageHasNoQueueId()
        {
            var api = CreateApiWithMessages();
            string? selected = null;

            var cut = RenderWithMudProvider<MessagesTab>(
                (nameof(MessagesTab.QueueId), TestQueueId),
                (nameof(MessagesTab.Api), api),
                (nameof(MessagesTab.OnMessageSelected), EventCallback.Factory.Create<string>(this, s => selected = s)));

            var table = cut.FindComponent<MudTable<MessageResponse>>();
            await cut.InvokeAsync(() => table.Instance.OnRowClick.InvokeAsync(
                new TableRowClickEventArgs<MessageResponse>(new MouseEventArgs(), null!, new MessageResponse())));

            Assert.IsNull(selected);
        }

        [TestMethod]
        public void RefreshVersionChange_ReloadsMessages()
        {
            var api = CreateApiWithMessages();

            var tab = RenderMessagesTab(api).FindComponent<MessagesTab>();
            tab.Render(ps => ps
                .Add(p => p.QueueId, TestQueueId)
                .Add(p => p.Api, api)
                .Add(p => p.RefreshVersion, 1));

            api.Received(2).GetMessagesAsync(TestQueueId, 0, 25, null);
        }

        [TestMethod]
        public void StatusFilterVersionChange_AppliesNewFilterFromFirstPage()
        {
            var api = CreateApiWithMessages();

            var tab = RenderMessagesTab(api).FindComponent<MessagesTab>();
            tab.Render(ps => ps
                .Add(p => p.QueueId, TestQueueId)
                .Add(p => p.Api, api)
                .Add(p => p.StatusFilter, 2)
                .Add(p => p.StatusFilterVersion, 1));

            api.Received(1).GetMessagesAsync(TestQueueId, 0, 25, 2);
        }

        private IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> RenderMessagesTab(IDashboardApiClient api)
        {
            return RenderWithMudProvider<MessagesTab>(
                (nameof(MessagesTab.QueueId), TestQueueId),
                (nameof(MessagesTab.Api), api));
        }

        private static IDashboardApiClient CreateApiWithMessages(long totalCount = 1, bool includeItem = true)
        {
            var api = Substitute.For<IDashboardApiClient>();
            var items = includeItem
                ? new List<MessageResponse>
                {
                    new()
                    {
                        QueueId = "abc-123",
                        Status = 0,
                        QueuedDateTime = DateTimeOffset.UtcNow
                    }
                }
                : new List<MessageResponse>();
            api.GetMessagesAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int?>())
                .Returns(new PagedResponse<MessageResponse>
                {
                    Items = items,
                    TotalCount = totalCount
                });
            return api;
        }
    }
}
