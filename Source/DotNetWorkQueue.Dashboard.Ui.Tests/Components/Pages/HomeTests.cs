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
using Bunit.TestDoubles;
using DotNetWorkQueue.Dashboard.Ui.Components.Pages;
using DotNetWorkQueue.Dashboard.Ui.Models;
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

        [TestMethod]
        public void RendersConnectionsTable_WhenSlugResolvesWithConnections()
        {
            var sources = TwoSources();
            var clients = RegisterDependenciesWithPerSourceClients(sources);
            clients["primary"].GetConnectionsAsync().Returns(new List<ConnectionResponse>
            {
                new() { Id = Guid.NewGuid(), DisplayName = "Conn1", QueueCount = 3 }
            });

            var cut = Render<Home>(ps => ps.Add(p => p.SourceSlug, "primary"));

            StringAssert.Contains(cut.Markup, "Conn1");
            StringAssert.Contains(cut.Markup, "Primary");
        }

        [TestMethod]
        public void ShowsNoConnectionsMessage_WhenSourceHasNoConnections()
        {
            var sources = TwoSources();
            var clients = RegisterDependenciesWithPerSourceClients(sources);
            clients["primary"].GetConnectionsAsync().Returns(new List<ConnectionResponse>());

            var cut = Render<Home>(ps => ps.Add(p => p.SourceSlug, "primary"));

            StringAssert.Contains(cut.Markup, "No connections registered");
        }

        [TestMethod]
        public void ShowsErrorMessage_WhenLoadConnectionsThrows()
        {
            var sources = TwoSources();
            var clients = RegisterDependenciesWithPerSourceClients(sources);
            // A single stub that throws once then succeeds — re-stubbing after the throw is
            // configured would re-invoke the throwing setup on the substitute.
            var callCount = 0;
            clients["primary"].GetConnectionsAsync().Returns<Task<List<ConnectionResponse>>>(_ =>
            {
                callCount++;
                if (callCount == 1) throw new InvalidOperationException("boom");
                return Task.FromResult(new List<ConnectionResponse>
                {
                    new() { Id = Guid.NewGuid(), DisplayName = "Conn1", QueueCount = 1 }
                });
            });

            var cut = Render<Home>(ps => ps.Add(p => p.SourceSlug, "primary"));

            StringAssert.Contains(cut.Markup, "Failed to load connections: boom");

            cut.Find("button").Click();

            StringAssert.Contains(cut.Markup, "Conn1");
        }

        [TestMethod]
        public void NavigatesToConnection_WhenConnectionRowClicked()
        {
            var sources = TwoSources();
            var clients = RegisterDependenciesWithPerSourceClients(sources);
            var connectionId = Guid.NewGuid();
            clients["primary"].GetConnectionsAsync().Returns(new List<ConnectionResponse>
            {
                new() { Id = connectionId, DisplayName = "Conn1", QueueCount = 1 }
            });
            var nav = Services.GetRequiredService<BunitNavigationManager>();

            var cut = Render<Home>(ps => ps.Add(p => p.SourceSlug, "primary"));
            cut.FindAll("tr")[1].Click();

            StringAssert.EndsWith(nav.Uri, $"/source/primary/connections/{connectionId}");
        }

        [TestMethod]
        public void SkipsReload_WhenSameSlugRenderedAgain()
        {
            var sources = TwoSources();
            var clients = RegisterDependenciesWithPerSourceClients(sources);
            clients["primary"].GetConnectionsAsync().Returns(new List<ConnectionResponse>());

            var cut = Render<Home>(ps => ps.Add(p => p.SourceSlug, "primary"));
            cut.Render(ps => ps.Add(p => p.SourceSlug, "primary"));

            clients["primary"].Received(1).GetConnectionsAsync();
        }

        [TestMethod]
        public void ShowsSourceUnreachable_WhenGroupUnhealthyWithNoConnectionsOrError()
        {
            var sources = TwoNamedSources("Alpha", "Beta");
            var healthBySlug = new Dictionary<string, SourceHealthStatus>
            {
                ["alpha"] = SourceHealthStatus.Unhealthy,
                ["beta"] = SourceHealthStatus.Healthy
            };
            var clients = RegisterDependenciesWithPerSourceClients(sources, healthBySlug);
            clients["alpha"].GetConnectionsAsync().Returns((List<ConnectionResponse>)null!);
            clients["beta"].GetConnectionsAsync().Returns(new List<ConnectionResponse>());

            var cut = Render<Home>();

            StringAssert.Contains(cut.Markup, "Source unreachable");

            clients["alpha"].GetConnectionsAsync().Returns(new List<ConnectionResponse>
            {
                new() { Id = Guid.NewGuid(), DisplayName = "Recovered", QueueCount = 2 }
            });
            cut.Find("button").Click();

            StringAssert.Contains(cut.Markup, "Recovered");
        }

        [TestMethod]
        public void ShowsErrorMessage_WhenGroupLoadThrows()
        {
            var sources = TwoNamedSources("Alpha", "Beta");
            var clients = RegisterDependenciesWithPerSourceClients(sources);
            clients["alpha"].GetConnectionsAsync()
                .Returns<Task<List<ConnectionResponse>>>(_ => throw new InvalidOperationException("group boom"));
            clients["beta"].GetConnectionsAsync().Returns(new List<ConnectionResponse>());

            var cut = Render<Home>();

            StringAssert.Contains(cut.Markup, "group boom");
        }

        [TestMethod]
        public void NavigatesToGroupConnection_WhenConnectionRowClicked()
        {
            var sources = TwoNamedSources("Alpha", "Beta");
            var clients = RegisterDependenciesWithPerSourceClients(sources);
            var connectionId = Guid.NewGuid();
            clients["alpha"].GetConnectionsAsync().Returns(new List<ConnectionResponse>
            {
                new() { Id = connectionId, DisplayName = "Conn1", QueueCount = 1 }
            });
            clients["beta"].GetConnectionsAsync().Returns(new List<ConnectionResponse>());
            var nav = Services.GetRequiredService<BunitNavigationManager>();

            var cut = Render<Home>();
            cut.Find("tbody tr").Click();

            StringAssert.EndsWith(nav.Uri, $"/source/alpha/connections/{connectionId}");
        }

        [TestMethod]
        public void RendersDefaultHealthIcon_WhenStatusUnknown()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                new() { Name = "Alpha", BaseUrl = "https://a" },
                new() { Name = "Beta", BaseUrl = "https://b" }
            };
            var healthBySlug = new Dictionary<string, SourceHealthStatus>
            {
                ["alpha"] = SourceHealthStatus.Unknown
            };
            var clients = RegisterDependenciesWithPerSourceClients(sources, healthBySlug);
            clients["alpha"].GetConnectionsAsync().Returns(new List<ConnectionResponse>());
            clients["beta"].GetConnectionsAsync().Returns(new List<ConnectionResponse>());

            var cut = Render<Home>();

            StringAssert.Contains(cut.Markup, "Alpha");
        }

        private static List<DashboardApiSourceConfig> TwoSources() => new()
        {
            new() { Name = "Primary", BaseUrl = "https://a" },
            new() { Name = "Secondary", BaseUrl = "https://b" }
        };

        private static List<DashboardApiSourceConfig> TwoNamedSources(string first, string second) => new()
        {
            new() { Name = first, BaseUrl = "https://a" },
            new() { Name = second, BaseUrl = "https://b" }
        };

        /// <summary>
        /// Like <see cref="RegisterDependencies"/>, but hands back a distinct
        /// <see cref="IDashboardApiClient"/> substitute per source slug so tests
        /// can drive per-source success/error/retry behavior independently, and
        /// optionally override the per-source health status.
        /// </summary>
        private Dictionary<string, IDashboardApiClient> RegisterDependenciesWithPerSourceClients(
            IReadOnlyList<DashboardApiSourceConfig> sources,
            IReadOnlyDictionary<string, SourceHealthStatus>? healthBySlug = null)
        {
            var registry = Substitute.For<ISourceRegistry>();
            registry.GetAll().Returns(sources);
            foreach (var src in sources)
                registry.GetBySlug(src.Slug).Returns(src);

            var health = Substitute.For<ISourceHealthMonitor>();
            foreach (var src in sources)
            {
                var status = healthBySlug != null && healthBySlug.TryGetValue(src.Slug, out var s)
                    ? s
                    : SourceHealthStatus.Healthy;
                health.GetHealth(src.Slug).Returns(new SourceHealthState { Status = status });
            }

            var clients = new Dictionary<string, IDashboardApiClient>();
            foreach (var src in sources)
                clients[src.Slug] = Substitute.For<IDashboardApiClient>();

            var multi = Substitute.For<IMultiSourceDashboardApiClient>();
            multi.GetClientForSource(Arg.Any<string>()).Returns(call =>
            {
                var slug = call.Arg<string>();
                if (clients.TryGetValue(slug, out var client))
                    return client;
                throw new KeyNotFoundException();
            });

            Services.AddSingleton(registry);
            Services.AddSingleton(health);
            Services.AddSingleton(multi);

            return clients;
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
