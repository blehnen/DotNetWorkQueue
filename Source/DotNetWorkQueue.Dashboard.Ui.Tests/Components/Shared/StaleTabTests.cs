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
    public class StaleTabTests : BunitTestBase
    {
        private static readonly Guid TestQueueId = Guid.NewGuid();
        private const string TestStaleMessageId = "stale-msg-0001";

        [TestMethod]
        public void LoadsStaleMessages_OnInitialization()
        {
            var api = CreateApiWithStaleMessages();
            var snackbar = Substitute.For<ISnackbar>();

            RenderStaleTab(api, snackbar);

            api.Received(1).GetStaleMessagesAsync(TestQueueId, 60, 0, 25);
        }

        [TestMethod]
        public void RendersRows_WhenStaleMessagesPresent()
        {
            var api = CreateApiWithStaleMessages();
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderStaleTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "Waiting");
        }

        [TestMethod]
        public void ShowsNoRecordsContent_WhenEmpty()
        {
            var api = CreateApiWithStaleMessages(totalCount: 0, includeItem: false);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderStaleTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "No stale messages found.");
        }

        [TestMethod]
        public void ShowsErrorAlert_WhenLoadStaleMessagesThrows()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetStaleMessagesAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns<Task<PagedResponse<MessageResponse>>>(_ => throw new InvalidOperationException("boom"));
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderStaleTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "boom");
        }

        [TestMethod]
        public void HidesActionButtons_WhenReadOnly()
        {
            var api = CreateApiWithStaleMessages(totalCount: 3);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderStaleTab(api, snackbar, readOnly: true);

            Assert.DoesNotContain("Reset", cut.Markup);
        }

        [TestMethod]
        public void ShowsActionButtons_WhenWritable()
        {
            var api = CreateApiWithStaleMessages(totalCount: 3);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderStaleTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "Reset All");
            StringAssert.Contains(cut.Markup, "Reset");
        }

        [TestMethod]
        public void ChangingThreshold_RequeriesWithNewValue()
        {
            var api = CreateApiWithStaleMessages();
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderStaleTab(api, snackbar);
            cut.Find("input").Change("120");

            api.Received(1).GetStaleMessagesAsync(TestQueueId, 120, 0, 25);
        }

        [TestMethod]
        public void ResetMessage_Success_ShowsSuccessSnackbarAndReloads()
        {
            var api = CreateApiWithStaleMessages();
            api.ResetMessageAsync(TestQueueId, TestStaleMessageId).Returns(true);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderStaleTab(api, snackbar);
            FindButton(cut, "Reset").Click();

            snackbar.Received(1).Add("Message reset.", Severity.Success);
            api.Received(2).GetStaleMessagesAsync(TestQueueId, 60, 0, 25);
        }

        [TestMethod]
        public void ResetMessage_Failure_ShowsErrorSnackbar()
        {
            var api = CreateApiWithStaleMessages();
            api.ResetMessageAsync(TestQueueId, TestStaleMessageId).Returns(false);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderStaleTab(api, snackbar);
            FindButton(cut, "Reset").Click();

            snackbar.Received(1).Add("Failed to reset.", Severity.Error);
        }

        [TestMethod]
        public void ResetAllStale_Success_ShowsSuccessSnackbarAndReloads()
        {
            var api = CreateApiWithStaleMessages(totalCount: 3);
            api.ResetAllStaleAsync(TestQueueId).Returns(new BulkActionResponse { Count = 3 });
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderStaleTab(api, snackbar);
            FindButton(cut, "Reset All").Click();
            FindButton(cut, "Click Again to Confirm").Click();

            snackbar.Received(1).Add("Reset 3 stale message(s).", Severity.Success);
            api.Received(2).GetStaleMessagesAsync(TestQueueId, 60, 0, 25);
        }

        [TestMethod]
        public void ResetAllStale_Failure_ShowsErrorSnackbar()
        {
            var api = CreateApiWithStaleMessages(totalCount: 3);
            api.ResetAllStaleAsync(TestQueueId)
                .Returns<Task<BulkActionResponse>>(_ => throw new InvalidOperationException("reset-all-boom"));
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderStaleTab(api, snackbar);
            FindButton(cut, "Reset All").Click();
            FindButton(cut, "Click Again to Confirm").Click();

            snackbar.Received(1).Add("Failed: reset-all-boom", Severity.Error);
        }

        private static AngleSharp.Dom.IElement FindButton(IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> cut, string text)
        {
            return cut.FindAll("button").First(b => b.TextContent.Trim() == text);
        }

        private IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> RenderStaleTab(IDashboardApiClient api, ISnackbar snackbar, bool readOnly = false)
        {
            return RenderWithMudProvider<StaleTab>(
                (nameof(StaleTab.QueueId), TestQueueId),
                (nameof(StaleTab.Api), api),
                (nameof(StaleTab.Snackbar), snackbar),
                (nameof(StaleTab.ReadOnly), readOnly));
        }

        private static IDashboardApiClient CreateApiWithStaleMessages(long totalCount = 1, bool includeItem = true)
        {
            var api = Substitute.For<IDashboardApiClient>();
            var items = includeItem
                ? new List<MessageResponse>
                {
                    new()
                    {
                        QueueId = TestStaleMessageId,
                        Status = 0,
                        QueuedDateTime = DateTimeOffset.UtcNow
                    }
                }
                : new List<MessageResponse>();
            api.GetStaleMessagesAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(new PagedResponse<MessageResponse>
                {
                    Items = items,
                    TotalCount = totalCount
                });
            return api;
        }
    }
}
