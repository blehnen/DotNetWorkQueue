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
using NSubstitute;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Components.Shared
{
    [TestClass]
    public class ConsumersTabTests : BunitTestBase
    {
        private static readonly Guid TestQueueId = Guid.NewGuid();

        [TestMethod]
        public void LoadsConsumers_OnInitialization()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetConsumersAsync(TestQueueId).Returns(new List<ConsumerInfoResponse>());

            RenderConsumersTab(api);

            api.Received(1).GetConsumersAsync(TestQueueId);
        }

        [TestMethod]
        public void ShowsEmptyMessage_WhenNoConsumers()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetConsumersAsync(TestQueueId).Returns(new List<ConsumerInfoResponse>());

            var cut = RenderConsumersTab(api);

            StringAssert.Contains(cut.Markup, "No consumers currently connected to this queue.");
        }

        [TestMethod]
        public void RendersConsumerRow_WhenConsumersPresent()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetConsumersAsync(TestQueueId).Returns(new List<ConsumerInfoResponse>
            {
                new()
                {
                    ConsumerId = Guid.NewGuid(),
                    FriendlyName = "consumer-one",
                    MachineName = "machine-a",
                    ProcessId = 1234,
                    RegisteredAt = DateTimeOffset.UtcNow.AddHours(-2),
                    LastHeartbeat = DateTimeOffset.UtcNow,
                    MessagesProcessed = 10,
                    MessagesErrored = 1,
                    MessagesRolledBack = 2,
                    PoisonMessages = 0
                }
            });

            var cut = RenderConsumersTab(api);

            StringAssert.Contains(cut.Markup, "consumer-one");
            StringAssert.Contains(cut.Markup, "machine-a");
        }

        [TestMethod]
        public void RendersConsumerIdPrefix_WhenFriendlyNameMissing()
        {
            var consumerId = Guid.NewGuid();
            var api = Substitute.For<IDashboardApiClient>();
            api.GetConsumersAsync(TestQueueId).Returns(new List<ConsumerInfoResponse>
            {
                new()
                {
                    ConsumerId = consumerId,
                    FriendlyName = null,
                    MachineName = "machine-b",
                    ProcessId = 5678,
                    RegisteredAt = DateTimeOffset.UtcNow,
                    LastHeartbeat = DateTimeOffset.UtcNow,
                    MessagesProcessed = 0,
                    MessagesErrored = 0,
                    MessagesRolledBack = 0,
                    PoisonMessages = 0
                }
            });

            var cut = RenderConsumersTab(api);

            StringAssert.Contains(cut.Markup, consumerId.ToString("N")[..8]);
        }

        [TestMethod]
        public void ShowsErrorAlertAndRetryButton_WhenLoadFails()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetConsumersAsync(Arg.Any<Guid?>())
                .Returns<Task<List<ConsumerInfoResponse>>>(_ => throw new InvalidOperationException("consumer-load-failed"));

            var cut = RenderConsumersTab(api);

            StringAssert.Contains(cut.Markup, "consumer-load-failed");
            StringAssert.Contains(cut.Markup, "Retry");
        }

        [TestMethod]
        public void Retry_ReloadsConsumers_AfterFailure()
        {
            var api = Substitute.For<IDashboardApiClient>();
            var callCount = 0;
            api.GetConsumersAsync(Arg.Any<Guid?>()).Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("first-failure");
                return Task.FromResult(new List<ConsumerInfoResponse>());
            });

            var cut = RenderConsumersTab(api);
            StringAssert.Contains(cut.Markup, "first-failure");

            var retryButton = cut.Find("button");
            retryButton.Click();

            StringAssert.Contains(cut.Markup, "No consumers currently connected to this queue.");
            api.Received(2).GetConsumersAsync(TestQueueId);
        }

        [TestMethod]
        public void ShowsSpinner_WhileLoadIsInFlight()
        {
            var gate = new TaskCompletionSource<List<ConsumerInfoResponse>>();
            var api = Substitute.For<IDashboardApiClient>();
            api.GetConsumersAsync(Arg.Any<Guid?>()).Returns(gate.Task);

            var cut = RenderConsumersTab(api);

            StringAssert.Contains(cut.Markup, "mud-progress-circular");

            gate.SetResult(new List<ConsumerInfoResponse>());
            cut.WaitForAssertion(() =>
                StringAssert.Contains(cut.Markup, "No consumers currently connected to this queue."));
        }

        [TestMethod]
        public void FormatsUptimeInDays_WhenConsumerRegisteredOverADayAgo()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetConsumersAsync(TestQueueId).Returns(new List<ConsumerInfoResponse>
            {
                new()
                {
                    ConsumerId = Guid.NewGuid(),
                    FriendlyName = "long-running",
                    MachineName = "machine-c",
                    RegisteredAt = DateTimeOffset.Now.AddDays(-2).AddHours(-3),
                    LastHeartbeat = DateTimeOffset.UtcNow
                }
            });

            var cut = RenderConsumersTab(api);

            StringAssert.Contains(cut.Markup, "2d 3h");
        }

        [TestMethod]
        public void FormatsUptimeInMinutes_WhenConsumerRegisteredRecently()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetConsumersAsync(TestQueueId).Returns(new List<ConsumerInfoResponse>
            {
                new()
                {
                    ConsumerId = Guid.NewGuid(),
                    FriendlyName = "just-started",
                    MachineName = "machine-d",
                    RegisteredAt = DateTimeOffset.Now.AddMinutes(-5),
                    LastHeartbeat = DateTimeOffset.UtcNow
                }
            });

            var cut = RenderConsumersTab(api);

            StringAssert.Contains(cut.Markup, "5m");
        }

        [TestMethod]
        public void RefreshVersionChange_ReloadsConsumers()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetConsumersAsync(TestQueueId).Returns(new List<ConsumerInfoResponse>());

            var tab = RenderConsumersTab(api).FindComponent<ConsumersTab>();
            tab.Render(ps => ps
                .Add(p => p.QueueId, TestQueueId)
                .Add(p => p.Api, api)
                .Add(p => p.RefreshVersion, 1));

            api.Received(2).GetConsumersAsync(TestQueueId);
        }

        private IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> RenderConsumersTab(IDashboardApiClient api)
        {
            return RenderWithMudProvider<ConsumersTab>(
                (nameof(ConsumersTab.QueueId), TestQueueId),
                (nameof(ConsumersTab.Api), api));
        }
    }
}
