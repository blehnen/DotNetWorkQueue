// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.Factory
{
    /// <summary>
    /// Creates new instance of the options classes
    /// </summary>
    internal class PostgreSqlMessageQueueTransportOptionsFactory : IPostgreSqlMessageQueueTransportOptionsFactory
    {
        private readonly IQueryHandler<GetQueueOptionsQuery, PostgreSqlMessageQueueTransportOptions> _queryOptions;
        private readonly IConnectionInformation _connectionInformation;
        private readonly object _creator = new object();
        private PostgreSqlMessageQueueTransportOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlMessageQueueTransportOptionsFactory"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="queryOptions">The query options.</param>
        public PostgreSqlMessageQueueTransportOptionsFactory(IConnectionInformation connectionInformation,
            IQueryHandler<GetQueueOptionsQuery, PostgreSqlMessageQueueTransportOptions> queryOptions)
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
        public PostgreSqlMessageQueueTransportOptions Create()
        {
            if (string.IsNullOrEmpty(_connectionInformation.ConnectionString))
            {
                return new PostgreSqlMessageQueueTransportOptions();
            }

            if (_options != null) return _options;
            lock (_creator)
            {
                if (_options == null)
                {
                    _options = _queryOptions.Handle(new GetQueueOptionsQuery());
                }
                if (_options == null) //does not exist in DB; return a new copy
                {
                    _options = new PostgreSqlMessageQueueTransportOptions();
                }
            }
            return _options;
        }
    }
}
