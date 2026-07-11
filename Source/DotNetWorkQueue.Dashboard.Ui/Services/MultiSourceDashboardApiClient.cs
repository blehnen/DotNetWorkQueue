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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;

namespace DotNetWorkQueue.Dashboard.Ui.Services
{
    /// <summary>
    /// Multi-source client that returns cached <see cref="IDashboardApiClient"/> instances
    /// per API source, identified by slug. Uses <see cref="IHttpClientFactory"/> for
    /// named HttpClient creation and <see cref="ConcurrentDictionary{TKey,TValue}"/>
    /// for thread-safe caching.
    /// </summary>
    public class MultiSourceDashboardApiClient : IMultiSourceDashboardApiClient
    {
        private readonly ISourceRegistry _sourceRegistry;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ConcurrentDictionary<string, IDashboardApiClient> _clients = new();

        /// <summary>
        /// Creates a new <see cref="MultiSourceDashboardApiClient"/>.
        /// </summary>
        /// <param name="sourceRegistry">The registry of configured API sources.</param>
        /// <param name="httpClientFactory">The factory for creating named HttpClient instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public MultiSourceDashboardApiClient(ISourceRegistry sourceRegistry, IHttpClientFactory httpClientFactory)
        {
            ArgumentNullException.ThrowIfNull(sourceRegistry);
            ArgumentNullException.ThrowIfNull(httpClientFactory);

            _sourceRegistry = sourceRegistry;
            _httpClientFactory = httpClientFactory;
        }

        /// <inheritdoc />
        public IDashboardApiClient GetClientForSource(string slug)
        {
            ArgumentNullException.ThrowIfNull(slug);
            return _clients.GetOrAdd(slug, CreateClient);
        }

        /// <inheritdoc />
        public IReadOnlyList<DashboardApiSourceConfig> GetAllSources()
        {
            return _sourceRegistry.GetAll();
        }

        private IDashboardApiClient CreateClient(string slug)
        {
            _ = _sourceRegistry.GetBySlug(slug)
                ?? throw new KeyNotFoundException($"No API source configured with slug '{slug}'");

            var httpClient = _httpClientFactory.CreateClient(slug);
            return new DashboardApiClient(httpClient);
        }
    }
}
