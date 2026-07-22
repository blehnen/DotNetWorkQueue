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
