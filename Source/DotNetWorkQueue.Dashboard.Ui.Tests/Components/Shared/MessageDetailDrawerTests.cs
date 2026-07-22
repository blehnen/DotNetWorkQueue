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
using AngleSharp.Dom;
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
    public class MessageDetailDrawerTests : BunitTestBase
    {
        private static readonly Guid TestQueueId = Guid.NewGuid();
        private const string TestMessageId = "msg-0001";

        [TestMethod]
        public void DoesNotLoad_WhenDrawerClosed()
        {
            var api = CreateApi();

            RenderDrawer(api, open: false);

            api.DidNotReceive().GetMessageDetailAsync(Arg.Any<Guid>(), Arg.Any<string>());
        }

        [TestMethod]
        public void DoesNotLoad_WhenMessageIdNull()
        {
            var api = CreateApi();

            RenderDrawer(api, messageId: null);

            api.DidNotReceive().GetMessageDetailAsync(Arg.Any<Guid>(), Arg.Any<string>());
        }

        [TestMethod]
        public void RendersMetadata_WhenMessageLoaded()
        {
            var api = CreateApi(message: new MessageResponse
            {
                QueueId = TestMessageId,
                CorrelationId = "corr-99",
                Status = 0,
                Priority = 4,
                QueuedDateTime = DateTimeOffset.UtcNow
            });

            var cut = RenderDrawer(api);

            StringAssert.Contains(cut.Markup, TestMessageId);
            StringAssert.Contains(cut.Markup, "corr-99");
            StringAssert.Contains(cut.Markup, "Waiting");
            StringAssert.Contains(cut.Markup, "4");
        }

        [TestMethod]
        public void RendersPlaceholders_WhenOptionalMetadataMissing()
        {
            var api = CreateApi(message: new MessageResponse { QueueId = TestMessageId, Status = 3 });

            var cut = RenderDrawer(api);

            StringAssert.Contains(cut.Markup, "Processed");
            Assert.DoesNotContain("Process Time", cut.Markup);
            Assert.DoesNotContain("Expires", cut.Markup);
            Assert.DoesNotContain("Route", cut.Markup);
        }

        [TestMethod]
        public void RendersOptionalMetadata_WhenPresent()
        {
            var api = CreateApi(message: new MessageResponse
            {
                QueueId = TestMessageId,
                Status = 7,
                QueueProcessTime = DateTimeOffset.UtcNow,
                ExpirationTime = DateTimeOffset.UtcNow.AddHours(1),
                Route = "route-a"
            });

            var cut = RenderDrawer(api);

            StringAssert.Contains(cut.Markup, "Process Time");
            StringAssert.Contains(cut.Markup, "Expires");
            StringAssert.Contains(cut.Markup, "route-a");
            StringAssert.Contains(cut.Markup, "Unknown");
        }

        [TestMethod]
        public void ShowsDetailError_WhenDetailLoadThrows()
        {
            var api = CreateApi();
            api.GetMessageDetailAsync(Arg.Any<Guid>(), Arg.Any<string>())
                .Returns<Task<MessageResponse?>>(_ => throw new InvalidOperationException("detail boom"));

            var cut = RenderDrawer(api);

            StringAssert.Contains(cut.Markup, "detail boom");
        }

        [TestMethod]
        public void SwallowsFailures_FromBodyHeadersAndRetries()
        {
            var api = CreateApi();
            api.GetMessageBodyAsync(Arg.Any<Guid>(), Arg.Any<string>())
                .Returns<Task<MessageBodyResponse?>>(_ => throw new InvalidOperationException("body boom"));
            api.GetMessageHeadersAsync(Arg.Any<Guid>(), Arg.Any<string>())
                .Returns<Task<MessageHeadersResponse?>>(_ => throw new InvalidOperationException("headers boom"));
            api.GetMessageRetriesAsync(Arg.Any<Guid>(), Arg.Any<string>())
                .Returns<Task<List<ErrorRetryResponse>>>(_ => throw new InvalidOperationException("retries boom"));

            var cut = RenderDrawer(api);

            Assert.DoesNotContain("body boom", cut.Markup);
            Assert.DoesNotContain("headers boom", cut.Markup);
            Assert.DoesNotContain("retries boom", cut.Markup);
            StringAssert.Contains(cut.Markup, "No headers.");
        }

        [TestMethod]
        public void PrettyPrintsJsonBody()
        {
            var api = CreateApi(body: new MessageBodyResponse { Body = "{\"a\":1}" });

            var cut = RenderDrawer(api);

            StringAssert.Contains(cut.Markup, "\"a\": 1");
        }

        [TestMethod]
        public void RendersBodyVerbatim_WhenNotValidJson()
        {
            var api = CreateApi(body: new MessageBodyResponse { Body = "not json at all" });

            var cut = RenderDrawer(api);

            StringAssert.Contains(cut.Markup, "not json at all");
        }

        [TestMethod]
        public void RendersEmptyMarker_WhenBodyBlank()
        {
            var api = CreateApi(body: new MessageBodyResponse { Body = "   " });

            var cut = RenderDrawer(api);

            StringAssert.Contains(cut.Markup, "(empty)");
        }

        [TestMethod]
        public void RendersBodyDecodingErrorAndTypeAndInterceptors()
        {
            var api = CreateApi(body: new MessageBodyResponse
            {
                Body = "{}",
                DecodingError = "could not decode",
                TypeName = "Acme.Order",
                WasIntercepted = true,
                InterceptorChain = new List<string> { "Gzip", "Aes" }
            });

            var cut = RenderDrawer(api);

            StringAssert.Contains(cut.Markup, "could not decode");
            StringAssert.Contains(cut.Markup, "Acme.Order");
            StringAssert.Contains(cut.Markup, "Gzip &gt; Aes");
        }

        [TestMethod]
        public void OmitsInterceptorChain_WhenNotIntercepted()
        {
            var api = CreateApi(body: new MessageBodyResponse
            {
                Body = "{}",
                WasIntercepted = false,
                InterceptorChain = new List<string> { "Gzip" }
            });

            var cut = RenderDrawer(api);

            Assert.DoesNotContain("Interceptors:", cut.Markup);
        }

        [TestMethod]
        public void RendersHeaderRows_WhenHeadersPresent()
        {
            var api = CreateApi(headers: new MessageHeadersResponse
            {
                Headers = new Dictionary<string, object> { ["trace-id"] = "abc123" }
            });

            var cut = RenderDrawer(api);

            StringAssert.Contains(cut.Markup, "trace-id");
            StringAssert.Contains(cut.Markup, "abc123");
        }

        [TestMethod]
        public void RendersHeaderDecodingError_WhenNoHeadersButErrorPresent()
        {
            var api = CreateApi(headers: new MessageHeadersResponse { DecodingError = "bad headers" });

            var cut = RenderDrawer(api);

            StringAssert.Contains(cut.Markup, "bad headers");
        }

        [TestMethod]
        public void RendersRetryRows_WhenRetriesPresent()
        {
            var api = CreateApi(retries: new List<ErrorRetryResponse>
            {
                new() { ExceptionType = "System.TimeoutException", RetryCount = 3 }
            });

            var cut = RenderDrawer(api);

            StringAssert.Contains(cut.Markup, "System.TimeoutException");
            StringAssert.Contains(cut.Markup, "Error Retries");
        }

        [TestMethod]
        public void OmitsRetrySection_WhenNoRetries()
        {
            var api = CreateApi();

            var cut = RenderDrawer(api);

            Assert.DoesNotContain("Error Retries", cut.Markup);
        }

        [TestMethod]
        public void HidesEditAndActions_WhenReadOnly()
        {
            var api = CreateApi(message: WaitingMessage());

            var cut = RenderDrawer(api, readOnly: true);

            Assert.DoesNotContain("Edit Body", cut.Markup);
            Assert.DoesNotContain("Delete", cut.Markup);
        }

        [TestMethod]
        public void HidesEditButton_WhenStatusNotEditable()
        {
            var api = CreateApi(message: new MessageResponse { QueueId = TestMessageId, Status = 3 });

            var cut = RenderDrawer(api);

            Assert.DoesNotContain("Edit Body", cut.Markup);
        }

        [TestMethod]
        public void StartEdit_ShowsEditorPrefilledWithFormattedBody()
        {
            var api = CreateApi(message: WaitingMessage(), body: new MessageBodyResponse { Body = "{\"a\":1}" });

            var cut = RenderDrawer(api);
            ClickButton(cut, "Edit Body");

            var field = cut.FindComponent<MudTextField<string>>();
            StringAssert.Contains(field.Instance.Value, "\"a\": 1");
            StringAssert.Contains(cut.Markup, "Cancel");
        }

        [TestMethod]
        public void CancelEdit_HidesEditor()
        {
            var api = CreateApi(message: WaitingMessage());

            var cut = RenderDrawer(api);
            ClickButton(cut, "Edit Body");
            ClickButton(cut, "Cancel");

            Assert.HasCount(0, cut.FindComponents<MudTextField<string>>());
            StringAssert.Contains(cut.Markup, "Edit Body");
        }

        [TestMethod]
        public async Task SaveBody_DoesNothing_WhenEditorBlank()
        {
            var api = CreateApi(message: WaitingMessage(), body: new MessageBodyResponse { Body = "{\"a\":1}" });

            var cut = RenderDrawer(api);
            ClickButton(cut, "Edit Body");
            var field = cut.FindComponent<MudTextField<string>>();
            await cut.InvokeAsync(() => field.Instance.ValueChanged.InvokeAsync("   "));
            ClickButton(cut, "Save");

            api.DidNotReceive().UpdateMessageBodyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<EditMessageBodyRequest>());
        }

        [TestMethod]
        public void SaveBody_Success_ClosesEditorAndReloadsBody()
        {
            var snackbar = Substitute.For<ISnackbar>();
            var api = CreateApi(message: WaitingMessage(), body: new MessageBodyResponse { Body = "{\"a\":1}" });
            api.UpdateMessageBodyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<EditMessageBodyRequest>()).Returns(true);

            var cut = RenderDrawer(api, snackbar: snackbar);
            ClickButton(cut, "Edit Body");
            ClickButton(cut, "Save");

            snackbar.Received().Add("Body updated.", Severity.Success, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
            api.Received(2).GetMessageBodyAsync(TestQueueId, TestMessageId);
            StringAssert.Contains(cut.Markup, "Edit Body");
        }

        [TestMethod]
        public void SaveBody_Failure_ShowsErrorSnackbar()
        {
            var snackbar = Substitute.For<ISnackbar>();
            var api = CreateApi(message: WaitingMessage(), body: new MessageBodyResponse { Body = "{\"a\":1}" });
            api.UpdateMessageBodyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<EditMessageBodyRequest>()).Returns(false);

            var cut = RenderDrawer(api, snackbar: snackbar);
            ClickButton(cut, "Edit Body");
            ClickButton(cut, "Save");

            snackbar.Received().Add(
                "Failed to update. Message may be processing or JSON invalid.",
                Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        }

        [TestMethod]
        public void SaveBody_Throws_ShowsExceptionSnackbar()
        {
            var snackbar = Substitute.For<ISnackbar>();
            var api = CreateApi(message: WaitingMessage(), body: new MessageBodyResponse { Body = "{\"a\":1}" });
            api.UpdateMessageBodyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<EditMessageBodyRequest>())
                .Returns<Task<bool>>(_ => throw new InvalidOperationException("save boom"));

            var cut = RenderDrawer(api, snackbar: snackbar);
            ClickButton(cut, "Edit Body");
            ClickButton(cut, "Save");

            snackbar.Received().Add("Error: save boom", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        }

        [TestMethod]
        public void Delete_RequiresConfirmationClick()
        {
            var api = CreateApi(message: WaitingMessage());

            var cut = RenderDrawer(api);
            ClickButton(cut, "Delete");

            api.DidNotReceive().DeleteMessageAsync(Arg.Any<Guid>(), Arg.Any<string>());
            StringAssert.Contains(cut.Markup, "Click to Confirm");
        }

        [TestMethod]
        public void Delete_Confirmed_NotifiesAndClosesDrawer()
        {
            var snackbar = Substitute.For<ISnackbar>();
            var api = CreateApi(message: WaitingMessage());
            api.DeleteMessageAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(true);
            var dataChanged = false;
            bool? openChanged = null;

            var cut = RenderDrawer(api, snackbar: snackbar,
                onDataChanged: () => dataChanged = true,
                onOpenChanged: v => openChanged = v);
            ClickButton(cut, "Delete");
            ClickButton(cut, "Click to Confirm");

            api.Received(1).DeleteMessageAsync(TestQueueId, TestMessageId);
            Assert.IsTrue(dataChanged);
            Assert.AreEqual(false, openChanged);
        }

        [TestMethod]
        public void Delete_Failure_ShowsErrorSnackbar()
        {
            var snackbar = Substitute.For<ISnackbar>();
            var api = CreateApi(message: WaitingMessage());
            api.DeleteMessageAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);

            var cut = RenderDrawer(api, snackbar: snackbar);
            ClickButton(cut, "Delete");
            ClickButton(cut, "Click to Confirm");

            snackbar.Received().Add("Failed to delete.", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        }

        [TestMethod]
        public void Requeue_Success_ReloadsMessage()
        {
            var snackbar = Substitute.For<ISnackbar>();
            var api = CreateApi(message: ErrorMessage());
            api.RequeueMessageAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(true);
            var dataChanged = false;

            var cut = RenderDrawer(api, snackbar: snackbar, onDataChanged: () => dataChanged = true);
            ClickButton(cut, "Requeue");

            Assert.IsTrue(dataChanged);
            api.Received(2).GetMessageDetailAsync(TestQueueId, TestMessageId);
        }

        [TestMethod]
        public void Requeue_Failure_ShowsErrorSnackbar()
        {
            var snackbar = Substitute.For<ISnackbar>();
            var api = CreateApi(message: ErrorMessage());
            api.RequeueMessageAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);

            var cut = RenderDrawer(api, snackbar: snackbar);
            ClickButton(cut, "Requeue");

            snackbar.Received().Add("Failed to requeue.", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        }

        [TestMethod]
        public void Reset_Success_ReloadsMessage()
        {
            var snackbar = Substitute.For<ISnackbar>();
            var api = CreateApi(message: ProcessingMessage());
            api.ResetMessageAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(true);
            var dataChanged = false;

            var cut = RenderDrawer(api, snackbar: snackbar, onDataChanged: () => dataChanged = true);
            ClickButton(cut, "Reset");

            Assert.IsTrue(dataChanged);
            api.Received(2).GetMessageDetailAsync(TestQueueId, TestMessageId);
        }

        [TestMethod]
        public void Reset_Failure_ShowsErrorSnackbar()
        {
            var snackbar = Substitute.For<ISnackbar>();
            var api = CreateApi(message: ProcessingMessage());
            api.ResetMessageAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);

            var cut = RenderDrawer(api, snackbar: snackbar);
            ClickButton(cut, "Reset");

            snackbar.Received().Add("Failed to reset.", Severity.Error, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
        }

        [TestMethod]
        public void Cancel_Success_MarksRequestedAndIgnoresSecondClick()
        {
            var snackbar = Substitute.For<ISnackbar>();
            var api = CreateApi(message: ProcessingMessage());
            api.CancelMessageAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(true);

            var cut = RenderDrawer(api, snackbar: snackbar);
            ClickButton(cut, "Cancel");
            StringAssert.Contains(cut.Markup, "Requested");

            ClickButton(cut, "Requested");

            api.Received(1).CancelMessageAsync(TestQueueId, TestMessageId);
        }

        [TestMethod]
        public void Cancel_Failure_ShowsWarningSnackbar()
        {
            var snackbar = Substitute.For<ISnackbar>();
            var api = CreateApi(message: ProcessingMessage());
            api.CancelMessageAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);

            var cut = RenderDrawer(api, snackbar: snackbar);
            ClickButton(cut, "Cancel");

            snackbar.Received().Add(
                "Could not cancel — message may not be processing or no consumer is in-process.",
                Severity.Warning, Arg.Any<Action<SnackbarOptions>>(), Arg.Any<string>());
            Assert.DoesNotContain("Requested", cut.Markup);
        }

        [TestMethod]
        public void CloseButton_RaisesOpenChangedFalse()
        {
            var api = CreateApi(message: WaitingMessage());
            bool? openChanged = null;

            var cut = RenderDrawer(api, onOpenChanged: v => openChanged = v);
            cut.FindAll("button")[0].Click();

            Assert.AreEqual(false, openChanged);
        }

        [TestMethod]
        public void ReopeningWithSameMessageId_DoesNotReload()
        {
            var api = CreateApi(message: WaitingMessage());

            var cut = RenderDrawer(api);
            cut.Render(ps => ps
                .Add(p => p.QueueId, TestQueueId)
                .Add(p => p.MessageId, TestMessageId)
                .Add(p => p.Open, true)
                .Add(p => p.Api, api)
                .Add(p => p.Snackbar, Substitute.For<ISnackbar>()));

            api.Received(1).GetMessageDetailAsync(TestQueueId, TestMessageId);
        }

        [TestMethod]
        public void ClosingDrawer_ClearsLoadedIdSoReopenReloads()
        {
            var api = CreateApi(message: WaitingMessage());
            var snackbar = Substitute.For<ISnackbar>();

            var cut = RenderDrawer(api, snackbar: snackbar);
            cut.Render(ps => ps
                .Add(p => p.QueueId, TestQueueId)
                .Add(p => p.MessageId, TestMessageId)
                .Add(p => p.Open, false)
                .Add(p => p.Api, api)
                .Add(p => p.Snackbar, snackbar));
            cut.Render(ps => ps
                .Add(p => p.QueueId, TestQueueId)
                .Add(p => p.MessageId, TestMessageId)
                .Add(p => p.Open, true)
                .Add(p => p.Api, api)
                .Add(p => p.Snackbar, snackbar));

            api.Received(2).GetMessageDetailAsync(TestQueueId, TestMessageId);
        }

        private static MessageResponse WaitingMessage() =>
            new() { QueueId = TestMessageId, Status = 0 };

        private static MessageResponse ProcessingMessage() =>
            new() { QueueId = TestMessageId, Status = 1 };

        private static MessageResponse ErrorMessage() =>
            new() { QueueId = TestMessageId, Status = 2 };

        private static void ClickButton(IRenderedComponent<MessageDetailDrawer> cut, string text)
        {
            var button = cut.FindAll("button")
                .First(b => b.TextContent.Contains(text, StringComparison.Ordinal));
            button.Click();
        }

        private static IDashboardApiClient CreateApi(
            MessageResponse? message = null,
            MessageBodyResponse? body = null,
            MessageHeadersResponse? headers = null,
            List<ErrorRetryResponse>? retries = null)
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetMessageDetailAsync(Arg.Any<Guid>(), Arg.Any<string>())
                .Returns(message ?? new MessageResponse { QueueId = TestMessageId, Status = 0 });
            api.GetMessageBodyAsync(Arg.Any<Guid>(), Arg.Any<string>())
                .Returns(body ?? new MessageBodyResponse { Body = "{}" });
            api.GetMessageHeadersAsync(Arg.Any<Guid>(), Arg.Any<string>())
                .Returns(headers ?? new MessageHeadersResponse());
            api.GetMessageRetriesAsync(Arg.Any<Guid>(), Arg.Any<string>())
                .Returns(retries ?? new List<ErrorRetryResponse>());
            return api;
        }

        private IRenderedComponent<MessageDetailDrawer> RenderDrawer(
            IDashboardApiClient api,
            bool open = true,
            string? messageId = TestMessageId,
            bool readOnly = false,
            ISnackbar? snackbar = null,
            Action? onDataChanged = null,
            Action<bool>? onOpenChanged = null)
        {
            return Render<MessageDetailDrawer>(ps =>
            {
                ps.Add(p => p.QueueId, TestQueueId)
                  .Add(p => p.MessageId, messageId)
                  .Add(p => p.Open, open)
                  .Add(p => p.ReadOnly, readOnly)
                  .Add(p => p.Api, api)
                  .Add(p => p.Snackbar, snackbar ?? Substitute.For<ISnackbar>());
                if (onDataChanged != null) ps.Add(p => p.OnDataChanged, onDataChanged);
                if (onOpenChanged != null) ps.Add(p => p.OpenChanged, onOpenChanged);
            });
        }
    }
}
