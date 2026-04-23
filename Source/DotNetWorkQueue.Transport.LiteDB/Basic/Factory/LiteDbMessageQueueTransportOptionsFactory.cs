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
using DotNetWorkQueue.Transport.LiteDb.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.Factory
{
    /// <summary>
    /// Creates new instance of the options classes
    /// </summary>
    internal class LiteDbMessageQueueTransportOptionsFactory : ILiteDbMessageQueueTransportOptionsFactory
    {
        // Static fallback for in-memory mode where creation and consumer containers
        // have different LiteDB database instances
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, LiteDbMessageQueueTransportOptions>
            InMemoryOptionsCache = new System.Collections.Concurrent.ConcurrentDictionary<string, LiteDbMessageQueueTransportOptions>();

        private readonly IQueryHandler<GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>, LiteDbMessageQueueTransportOptions> _queryOptions;
        private readonly IConnectionInformation _connectionInformation;
        private readonly object _creator = new object();
        private LiteDbMessageQueueTransportOptions _options;
        private bool _loadedFromStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbMessageQueueTransportOptionsFactory"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="queryOptions">The query options.</param>
        public LiteDbMessageQueueTransportOptionsFactory(IConnectionInformation connectionInformation,
            IQueryHandler<GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>, LiteDbMessageQueueTransportOptions> queryOptions)
        {
            Guard.NotNull(() => queryOptions, queryOptions);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _queryOptions = queryOptions;
            _connectionInformation = connectionInformation;
        }

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <returns></returns>
        public LiteDbMessageQueueTransportOptions Create()
        {
            if (string.IsNullOrEmpty(_connectionInformation.ConnectionString))
            {
                return new LiteDbMessageQueueTransportOptions();
            }

            if (_loadedFromStore) return _options;
            lock (_creator)
            {
                if (_loadedFromStore) return _options;

                var loaded = _queryOptions.Handle(new GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>());
                if (loaded != null)
                {
                    _options = loaded;
                    _loadedFromStore = true;
                    return _options;
                }

                // Fallback: static cache for in-memory mode (producer/consumer with separate DB instances).
                // A cache hit here IS real persisted data, so we lock it in as the loaded value.
                var key = $"{_connectionInformation.QueueName}|{_connectionInformation.ConnectionString}";
                if (InMemoryOptionsCache.TryGetValue(key, out var cached))
                {
                    _options = cached;
                    _loadedFromStore = true;
                    return _options;
                }

                // Not in store, not in static cache — return a tentative default
                // whose reference is stable across calls, so callers that mutate the
                // returned instance (e.g. via the Creation class's Options property)
                // see their mutations persist. We re-query the store on every Create()
                // call until it returns non-null, at which point we swap the cached
                // reference to the loaded options.
                if (_options == null) _options = new LiteDbMessageQueueTransportOptions();
                return _options;
            }
        }

        /// <summary>
        /// Saves options to the static cache for in-memory mode fallback.
        /// </summary>
        internal static void SaveToCache(IConnectionInformation connectionInfo, LiteDbMessageQueueTransportOptions options)
        {
            var key = $"{connectionInfo.QueueName}|{connectionInfo.ConnectionString}";
            InMemoryOptionsCache[key] = options;
        }
    }
}
