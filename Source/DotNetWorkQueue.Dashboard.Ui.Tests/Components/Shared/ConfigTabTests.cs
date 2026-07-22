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
    public class ConfigTabTests : BunitTestBase
    {
        private static readonly Guid TestQueueId = Guid.NewGuid();

        [TestMethod]
        public void LoadsConfig_OnInitialization()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetQueueConfigurationAsync(TestQueueId).Returns(new ConfigurationResponse { ConfigurationJson = "{}" });

            RenderConfigTab(api);

            api.Received(1).GetQueueConfigurationAsync(TestQueueId);
        }

        [TestMethod]
        public void RendersFormattedJson_WhenConfigurationPresent()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetQueueConfigurationAsync(TestQueueId).Returns(new ConfigurationResponse { ConfigurationJson = "{\"key\":\"value\"}" });

            var cut = RenderConfigTab(api);

            StringAssert.Contains(cut.Markup, "\"key\"");
            StringAssert.Contains(cut.Markup, "\"value\"");
        }

        [TestMethod]
        public void RendersPlaceholder_WhenConfigurationJsonMissing()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetQueueConfigurationAsync(TestQueueId).Returns(new ConfigurationResponse { ConfigurationJson = null });

            var cut = RenderConfigTab(api);

            StringAssert.Contains(cut.Markup, "(no configuration)");
        }

        [TestMethod]
        public void RendersPlaceholder_WhenConfigResponseIsNull()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetQueueConfigurationAsync(TestQueueId).Returns((ConfigurationResponse?)null);

            var cut = RenderConfigTab(api);

            StringAssert.Contains(cut.Markup, "(no configuration)");
        }

        [TestMethod]
        public void RendersRawJson_WhenConfigurationJsonIsMalformed()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetQueueConfigurationAsync(TestQueueId).Returns(new ConfigurationResponse { ConfigurationJson = "not-json" });

            var cut = RenderConfigTab(api);

            StringAssert.Contains(cut.Markup, "not-json");
        }

        [TestMethod]
        public void ShowsErrorAlertAndRetryButton_WhenLoadFails()
        {
            var api = Substitute.For<IDashboardApiClient>();
            api.GetQueueConfigurationAsync(Arg.Any<Guid>())
                .Returns<Task<ConfigurationResponse?>>(_ => throw new InvalidOperationException("config-load-failed"));

            var cut = RenderConfigTab(api);

            StringAssert.Contains(cut.Markup, "config-load-failed");
            StringAssert.Contains(cut.Markup, "Retry");
        }

        [TestMethod]
        public void Retry_ReloadsConfig_AfterFailure()
        {
            var api = Substitute.For<IDashboardApiClient>();
            var callCount = 0;
            api.GetQueueConfigurationAsync(Arg.Any<Guid>()).Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("first-failure");
                return Task.FromResult<ConfigurationResponse?>(new ConfigurationResponse { ConfigurationJson = "{}" });
            });

            var cut = RenderConfigTab(api);
            StringAssert.Contains(cut.Markup, "first-failure");

            var retryButton = cut.Find("button");
            retryButton.Click();

            api.Received(2).GetQueueConfigurationAsync(TestQueueId);
        }

        private IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> RenderConfigTab(IDashboardApiClient api)
        {
            return RenderWithMudProvider<ConfigTab>(
                (nameof(ConfigTab.QueueId), TestQueueId),
                (nameof(ConfigTab.Api), api));
        }
    }
}
