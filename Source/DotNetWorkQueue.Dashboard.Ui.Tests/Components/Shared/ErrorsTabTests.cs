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
    public class ErrorsTabTests : BunitTestBase
    {
        private static readonly Guid TestQueueId = Guid.NewGuid();
        private const string TestErrorMessageId = "err-msg-0001";

        [TestMethod]
        public void LoadsErrors_OnInitialization()
        {
            var api = CreateApiWithErrors();
            var snackbar = Substitute.For<ISnackbar>();

            RenderErrorsTab(api, snackbar);

            api.Received(1).GetErrorsAsync(TestQueueId, 0, 25);
        }

        [TestMethod]
        public void RendersRows_WhenErrorsPresent()
        {
            var api = CreateApiWithErrors();
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "boom-exception");
        }

        [TestMethod]
        public void ShowsNoRecordsContent_WhenEmpty()
        {
            var api = CreateApiWithErrors(totalCount: 0, includeItem: false);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "No error messages.");
        }

        [TestMethod]
        public void ShowsErrorAlert_WhenLoadErrorsThrows()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetErrorsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns<Task<PagedResponse<ErrorMessageResponse>>>(_ => throw new InvalidOperationException("boom"));
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "boom");
        }

        [TestMethod]
        public void HidesActionButtons_WhenReadOnly()
        {
            var api = CreateApiWithErrors(totalCount: 3);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar, readOnly: true);

            Assert.DoesNotContain("Requeue", cut.Markup);
            Assert.DoesNotContain("Delete All", cut.Markup);
        }

        [TestMethod]
        public void ShowsActionButtons_WhenWritable()
        {
            var api = CreateApiWithErrors(totalCount: 3);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar);

            StringAssert.Contains(cut.Markup, "Requeue All");
            StringAssert.Contains(cut.Markup, "Delete All");
            StringAssert.Contains(cut.Markup, "Requeue");
            StringAssert.Contains(cut.Markup, "Delete");
        }

        [TestMethod]
        public void RequeueMessage_Success_ShowsSuccessSnackbarAndReloads()
        {
            var api = CreateApiWithErrors();
            api.RequeueMessageAsync(TestQueueId, TestErrorMessageId).Returns(true);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar);
            FindButton(cut, "Requeue").Click();

            snackbar.Received(1).Add("Message requeued.", Severity.Success);
            api.Received(2).GetErrorsAsync(TestQueueId, 0, 25);
        }

        [TestMethod]
        public void RequeueMessage_Failure_ShowsErrorSnackbar()
        {
            var api = CreateApiWithErrors();
            api.RequeueMessageAsync(TestQueueId, TestErrorMessageId).Returns(false);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar);
            FindButton(cut, "Requeue").Click();

            snackbar.Received(1).Add("Failed to requeue.", Severity.Error);
        }

        [TestMethod]
        public void DeleteMessage_Success_ShowsSuccessSnackbarAndReloads()
        {
            var api = CreateApiWithErrors();
            api.DeleteMessageAsync(TestQueueId, TestErrorMessageId).Returns(true);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar);
            FindButton(cut, "Delete").Click();
            FindButton(cut, "Confirm?").Click();

            snackbar.Received(1).Add("Message deleted.", Severity.Success);
            api.Received(2).GetErrorsAsync(TestQueueId, 0, 25);
        }

        [TestMethod]
        public void DeleteMessage_Failure_ShowsErrorSnackbar()
        {
            var api = CreateApiWithErrors();
            api.DeleteMessageAsync(TestQueueId, TestErrorMessageId).Returns(false);
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar);
            FindButton(cut, "Delete").Click();
            FindButton(cut, "Confirm?").Click();

            snackbar.Received(1).Add("Failed to delete.", Severity.Error);
        }

        [TestMethod]
        public void RequeueAllErrors_Success_ShowsSuccessSnackbarAndReloads()
        {
            var api = CreateApiWithErrors(totalCount: 3);
            api.RequeueAllErrorsAsync(TestQueueId).Returns(new BulkActionResponse { Count = 3 });
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar);
            FindButton(cut, "Requeue All").Click();
            FindButton(cut, "Click Again to Confirm").Click();

            snackbar.Received(1).Add("Requeued 3 error(s).", Severity.Success);
            api.Received(2).GetErrorsAsync(TestQueueId, 0, 25);
        }

        [TestMethod]
        public void RequeueAllErrors_Failure_ShowsErrorSnackbar()
        {
            var api = CreateApiWithErrors(totalCount: 3);
            api.RequeueAllErrorsAsync(TestQueueId)
                .Returns<Task<BulkActionResponse>>(_ => throw new InvalidOperationException("requeue-all-boom"));
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar);
            FindButton(cut, "Requeue All").Click();
            FindButton(cut, "Click Again to Confirm").Click();

            snackbar.Received(1).Add("Failed: requeue-all-boom", Severity.Error);
        }

        [TestMethod]
        public void DeleteAllErrors_Success_ShowsSuccessSnackbarAndReloads()
        {
            var api = CreateApiWithErrors(totalCount: 3);
            api.DeleteAllErrorsAsync(TestQueueId).Returns(new DeleteAllResponse { Deleted = 3 });
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar);
            FindButton(cut, "Delete All").Click();
            FindButton(cut, "Click Again to Confirm").Click();

            snackbar.Received(1).Add("Deleted 3 error(s).", Severity.Success);
            api.Received(2).GetErrorsAsync(TestQueueId, 0, 25);
        }

        [TestMethod]
        public void DeleteAllErrors_Failure_ShowsErrorSnackbar()
        {
            var api = CreateApiWithErrors(totalCount: 3);
            api.DeleteAllErrorsAsync(TestQueueId)
                .Returns<Task<DeleteAllResponse>>(_ => throw new InvalidOperationException("delete-all-boom"));
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderErrorsTab(api, snackbar);
            FindButton(cut, "Delete All").Click();
            FindButton(cut, "Click Again to Confirm").Click();

            snackbar.Received(1).Add("Failed: delete-all-boom", Severity.Error);
        }

        private static AngleSharp.Dom.IElement FindButton(IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> cut, string text)
        {
            return cut.FindAll("button").First(b => b.TextContent.Trim() == text);
        }

        private IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> RenderErrorsTab(IDashboardApiClient api, ISnackbar snackbar, bool readOnly = false)
        {
            return RenderWithMudProvider<ErrorsTab>(
                (nameof(ErrorsTab.QueueId), TestQueueId),
                (nameof(ErrorsTab.Api), api),
                (nameof(ErrorsTab.Snackbar), snackbar),
                (nameof(ErrorsTab.ReadOnly), readOnly));
        }

        private static IDashboardApiClient CreateApiWithErrors(long totalCount = 1, bool includeItem = true)
        {
            var api = Substitute.For<IDashboardApiClient>();
            var items = includeItem
                ? new List<ErrorMessageResponse>
                {
                    new()
                    {
                        QueueId = TestErrorMessageId,
                        LastException = "boom-exception",
                        LastExceptionDate = DateTimeOffset.UtcNow
                    }
                }
                : new List<ErrorMessageResponse>();
            api.GetErrorsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(new PagedResponse<ErrorMessageResponse>
                {
                    Items = items,
                    TotalCount = totalCount
                });
            return api;
        }
    }
}
