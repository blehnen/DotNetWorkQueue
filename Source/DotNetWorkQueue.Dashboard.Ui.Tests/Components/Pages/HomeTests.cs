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
using System.Collections.Generic;
using Bunit;
using Bunit.TestDoubles;
using DotNetWorkQueue.Dashboard.Ui.Components.Pages;
using DotNetWorkQueue.Dashboard.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Components.Pages
{
    [TestClass]
    public class HomeTests : BunitTestBase
    {
        [TestMethod]
        public void RedirectsToSingleSource_WhenNoSlugAndOneSourceConfigured()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                new() { Name = "Primary", BaseUrl = "https://example.com" }
            };
            RegisterDependencies(sources);
            var nav = Services.GetRequiredService<BunitNavigationManager>();

            Render<Home>();

            StringAssert.EndsWith(nav.Uri, "/source/primary");
        }

        [TestMethod]
        public void ShowsSourceNotFound_WhenSlugDoesNotResolve()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                new() { Name = "Primary", BaseUrl = "https://a" },
                new() { Name = "Secondary", BaseUrl = "https://b" }
            };
            RegisterDependencies(sources);

            var cut = Render<Home>(ps => ps.Add(p => p.SourceSlug, "does-not-exist"));

            StringAssert.Contains(cut.Markup, "not found");
        }

        [TestMethod]
        public void RendersMultiSourceView_WhenNoSlugAndMultipleSourcesConfigured()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                new() { Name = "Alpha", BaseUrl = "https://a" },
                new() { Name = "Beta", BaseUrl = "https://b" }
            };
            RegisterDependencies(sources);

            var cut = Render<Home>();

            StringAssert.Contains(cut.Markup, "Alpha");
            StringAssert.Contains(cut.Markup, "Beta");
        }

        private void RegisterDependencies(IReadOnlyList<DashboardApiSourceConfig> sources)
        {
            var registry = Substitute.For<ISourceRegistry>();
            registry.GetAll().Returns(sources);
            foreach (var src in sources)
                registry.GetBySlug(src.Slug).Returns(src);

            var health = Substitute.For<ISourceHealthMonitor>();
            foreach (var src in sources)
                health.GetHealth(src.Slug).Returns(new SourceHealthState { Status = SourceHealthStatus.Healthy });

            var multi = Substitute.For<IMultiSourceDashboardApiClient>();
            var apiClient = Substitute.For<IDashboardApiClient>();
            apiClient.GetConnectionsAsync().Returns(new List<DotNetWorkQueue.Dashboard.Ui.Models.ConnectionResponse>());
            var knownSlugs = new HashSet<string>();
            foreach (var src in sources)
                knownSlugs.Add(src.Slug);
            // Match production: known slugs return the client, unknown slugs throw KeyNotFoundException.
            multi.GetClientForSource(Arg.Any<string>()).Returns(call =>
            {
                var slug = call.Arg<string>();
                if (knownSlugs.Contains(slug))
                    return apiClient;
                throw new KeyNotFoundException();
            });

            Services.AddSingleton(registry);
            Services.AddSingleton(health);
            Services.AddSingleton(multi);
        }
    }
}
