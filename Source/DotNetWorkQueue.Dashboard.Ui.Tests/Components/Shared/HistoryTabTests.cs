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
using FluentAssertions;
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

            cut.Markup.Should().NotContain("Purge History");
        }

        [TestMethod]
        public void HidesPurgeButton_WhenNoRecords()
        {
            var api = CreateApiWithRecords(totalCount: 0);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);

            cut.Markup.Should().NotContain("Purge History");
        }

        [TestMethod]
        public void ShowsPurgeButton_WhenWritableAndHasRecords()
        {
            var api = CreateApiWithRecords(totalCount: 3);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);

            cut.Markup.Should().Contain("Purge History");
        }

        [TestMethod]
        public void ShowsErrorAlert_WhenLoadHistoryThrows()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetHistoryAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int?>())
                .Returns<Task<PagedResponse<HistoryResponse>>>(_ => throw new InvalidOperationException("boom"));
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderHistoryTab(api, snackbar);

            cut.Markup.Should().Contain("boom");
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

            cut.Markup.Should().Contain("Expand exception");
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
