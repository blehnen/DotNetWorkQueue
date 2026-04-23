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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.Factory
{
    /// <inheritdoc />
    internal class PostgreSqlMessageQueueTransportOptionsFactory : IPostgreSqlMessageQueueTransportOptionsFactory
    {
        private readonly IQueryHandler<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>, PostgreSqlMessageQueueTransportOptions> _queryOptions;
        private readonly IConnectionInformation _connectionInformation;
        private readonly object _creator = new object();
        private PostgreSqlMessageQueueTransportOptions _options;
        private bool _loadedFromStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlMessageQueueTransportOptionsFactory"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="queryOptions">The query options.</param>
        public PostgreSqlMessageQueueTransportOptionsFactory(IConnectionInformation connectionInformation,
            IQueryHandler<GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>, PostgreSqlMessageQueueTransportOptions> queryOptions)
        {
            Guard.NotNull(() => queryOptions, queryOptions);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _queryOptions = queryOptions;
            _connectionInformation = connectionInformation;
        }

        /// <inheritdoc />
        public PostgreSqlMessageQueueTransportOptions Create()
        {
            if (string.IsNullOrEmpty(_connectionInformation.ConnectionString))
            {
                return new PostgreSqlMessageQueueTransportOptions();
            }

            if (_loadedFromStore) return _options;
            lock (_creator)
            {
                if (_loadedFromStore) return _options;

                var loaded = _queryOptions.Handle(new GetQueueOptionsQuery<PostgreSqlMessageQueueTransportOptions>());
                if (loaded != null)
                {
                    _options = loaded;
                    _loadedFromStore = true;
                    return _options;
                }

                // Queue does not yet exist in the store — return a tentative default
                // whose reference is stable across calls, so callers that mutate the
                // returned instance (e.g. via the Creation class's Options property)
                // see their mutations persist. We re-query the store on every Create()
                // call until it returns non-null, at which point we swap the cached
                // reference to the loaded options.
                if (_options == null) _options = new PostgreSqlMessageQueueTransportOptions();
                return _options;
            }
        }
    }
}
