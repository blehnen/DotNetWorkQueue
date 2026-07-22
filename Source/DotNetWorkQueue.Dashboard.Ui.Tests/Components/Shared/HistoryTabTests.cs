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
    public class HistoryTabTests : BunitTestBase
    {
        private static readonly Guid TestQueueId = Guid.NewGuid();

        [TestMethod]
        public void LoadsHistory_OnInitialization()
        {
            var api = CreateApiWithRecords();
            var snackbar = Substitute.For<ISnackbar>();

            RenderHistoryTab(api, snackbar);

            api.Received(1).GetHistoryAsync(TestQueueId, 0, 25, null);
        }

        [TestMethod]
        public void HidesPurgeButton_WhenReadOnly()
        {
            var api = CreateApiWithRecords(totalCount: 5);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar, readOnly: true);

            Assert.DoesNotContain("Purge History", cut.Markup);
        }

        [TestMethod]
        public void HidesPurgeButton_WhenNoRecords()
        {
            var api = CreateApiWithRecords(totalCount: 0);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);

            Assert.DoesNotContain("Purge History", cut.Markup);
        }

        [TestMethod]
        public void ShowsPurgeButton_WhenWritableAndHasRecords()
        {
            var api = CreateApiWithRecords(totalCount: 3);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "Purge History");
        }

        [TestMethod]
        public void ShowsErrorAlert_WhenLoadHistoryThrows()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetHistoryAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int?>())
                .Returns<Task<PagedResponse<HistoryResponse>>>(_ => throw new InvalidOperationException("boom"));
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "boom");
        }

        [TestMethod]
        public void RendersExpandControl_ForRecordsWithExceptions()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetHistoryAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int?>())
                .Returns(new PagedResponse<HistoryResponse>
                {
                    Items = new List<HistoryResponse>
                    {
                        new()
                        {
                            QueueId = "abc-123",
                            Status = 3,
                            EnqueuedUtc = DateTime.UtcNow,
                            ExceptionText = "SpecificErrorMarker"
                        }
                    },
                    TotalCount = 1
                });
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "Expand exception");
        }

        [TestMethod]
        public void RendersNoRecordsContent_WhenEmpty()
        {
            var api = CreateApiWithRecords(totalCount: 0);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "No history records found.");
        }

        [TestMethod]
        public void RendersRowValues_ForAllStatusesAndDurationRanges()
        {
            var api = CreateApiWithItems(new List<HistoryResponse>
            {
                Record("enq-0000000001", 0, durationMs: null),
                Record("proc-000000001", 1, durationMs: 0),
                Record("comp-000000001", 2, durationMs: 250),
                Record("err-0000000001", 3, durationMs: 1500, retryCount: 2, route: "route-x", messageType: "Acme.Order"),
                Record("del-0000000001", 4, durationMs: 120000),
                Record("exp-0000000001", 5, durationMs: 59999),
                Record("unk-0000000001", 9, durationMs: 999)
            });
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "Enqueued");
            StringAssert.Contains(cut.Markup, "Complete");
            StringAssert.Contains(cut.Markup, "Expired");
            StringAssert.Contains(cut.Markup, "Unknown");
            StringAssert.Contains(cut.Markup, "&lt; 1 ms");
            StringAssert.Contains(cut.Markup, "250ms");
            StringAssert.Contains(cut.Markup, "1.5s");
            StringAssert.Contains(cut.Markup, "2.0m");
            StringAssert.Contains(cut.Markup, "route-x");
            StringAssert.Contains(cut.Markup, "Acme.Order");
            StringAssert.Contains(cut.Markup, "enq-00000000...");
        }

        [TestMethod]
        public void RendersDash_WhenQueueIdMissing()
        {
            var api = CreateApiWithItems(new List<HistoryResponse>
            {
                new() { QueueId = null, Status = 2, EnqueuedUtc = DateTime.UtcNow }
            });
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "-");
        }

        [TestMethod]
        public void ExpandingRow_ShowsExceptionText_AndCollapsingHidesIt()
        {
            var api = CreateApiWithItems(new List<HistoryResponse>
            {
                Record("err-0000000001", 3, exceptionText: "SpecificErrorMarker")
            });
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);
            cut.Find("button[aria-label='Expand exception']").Click();

            StringAssert.Contains(cut.Markup, "SpecificErrorMarker");

            cut.Find("button[aria-label='Collapse exception']").Click();

            Assert.DoesNotContain("SpecificErrorMarker", cut.Markup);
        }

        [TestMethod]
        public async Task ChangingStatusFilter_RequeriesFromFirstPage()
        {
            var api = CreateApiWithRecords(totalCount: 1);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);
            var select = cut.FindComponent<MudSelect<int?>>();
            await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(3));

            await api.Received(1).GetHistoryAsync(TestQueueId, 0, 25, 3);
        }

        [TestMethod]
        public async Task ChangingPage_RequeriesWithZeroBasedIndex()
        {
            var api = CreateApiWithRecords(totalCount: 60);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);
            var pagination = cut.FindComponent<MudPagination>();
            await cut.InvokeAsync(() => pagination.Instance.SelectedChanged.InvokeAsync(3));

            await api.Received(1).GetHistoryAsync(TestQueueId, 2, 25, null);
        }

        [TestMethod]
        public void RefreshVersionChange_ReloadsHistory()
        {
            var api = CreateApiWithRecords();
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar).FindComponent<HistoryTab>();
            cut.Render(ps => ps
                .Add(p => p.QueueId, TestQueueId)
                .Add(p => p.Api, api)
                .Add(p => p.Snackbar, snackbar)
                .Add(p => p.RefreshVersion, 1));

            api.Received(2).GetHistoryAsync(TestQueueId, 0, 25, null);
        }

        [TestMethod]
        public void PurgeHistory_RequiresConfirmation_ThenPurgesAndReloads()
        {
            var api = CreateApiWithRecords(totalCount: 3);
            api.PurgeHistoryAsync(Arg.Any<Guid>(), Arg.Any<int?>()).Returns(new DeleteAllResponse { Deleted = 7 });
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);
            cut.Find("button.mud-button-filled").Click();

            api.DidNotReceive().PurgeHistoryAsync(Arg.Any<Guid>(), Arg.Any<int?>());
            StringAssert.Contains(cut.Markup, "Click Again to Confirm");

            cut.Find("button.mud-button-filled").Click();

            api.Received(1).PurgeHistoryAsync(TestQueueId, 30);
            snackbar.Received().Add("Purged 7 record(s).", Severity.Success, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
            api.Received(2).GetHistoryAsync(TestQueueId, 0, 25, null);
        }

        [TestMethod]
        public void PurgeHistory_Failure_ShowsErrorSnackbar()
        {
            var api = CreateApiWithRecords(totalCount: 3);
            api.PurgeHistoryAsync(Arg.Any<Guid>(), Arg.Any<int?>())
                .Returns<Task<DeleteAllResponse>>(_ => throw new InvalidOperationException("purge boom"));
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);
            cut.Find("button.mud-button-filled").Click();
            cut.Find("button.mud-button-filled").Click();

            snackbar.Received().Add("Failed: purge boom", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        }

        private static HistoryResponse Record(
            string queueId,
            int status,
            long? durationMs = null,
            int retryCount = 0,
            string? route = null,
            string? messageType = null,
            string? exceptionText = null) =>
            new()
            {
                QueueId = queueId,
                Status = status,
                EnqueuedUtc = DateTime.UtcNow,
                DurationMs = durationMs,
                RetryCount = retryCount,
                Route = route,
                MessageType = messageType,
                ExceptionText = exceptionText
            };

        private static IDashboardApiClient CreateApiWithItems(List<HistoryResponse> items)
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetHistoryAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int?>())
                .Returns(new PagedResponse<HistoryResponse> { Items = items, TotalCount = items.Count });
            return api;
        }

        private IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> RenderHistoryTab(IDashboardApiClient api, ISnackbar snackbar, bool readOnly = false)
        {
            return RenderWithMudProvider<HistoryTab>(
                (nameof(HistoryTab.QueueId), TestQueueId),
                (nameof(HistoryTab.Api), api),
                (nameof(HistoryTab.Snackbar), snackbar),
                (nameof(HistoryTab.ReadOnly), readOnly));
        }

        private static IDashboardApiClient CreateApiWithRecords(long totalCount = 1)
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetHistoryAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int?>())
                .Returns(new PagedResponse<HistoryResponse>
                {
                    Items = new List<HistoryResponse>(),
                    TotalCount = totalCount
                });
            return api;
        }
    }
}
