// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Returns the current retry count for a message and a specific exception type
    /// </summary>
    internal class GetErrorRetryCountQueryHandler<T> : IQueryHandler<GetErrorRetryCountQuery<T>, int>
    {
        private readonly IPrepareQueryHandler<GetErrorRetryCountQuery<T>, int> _prepareQuery;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IReadColumn _readColumn;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetErrorRetryCountQueryHandler{T}" /> class.
        /// </summary>
        /// <param name="prepareQuery">The prepare query.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="readColumn">The read column.</param>
        public GetErrorRetryCountQueryHandler(IPrepareQueryHandler<GetErrorRetryCountQuery<T>, int> prepareQuery, 
            IDbConnectionFactory connectionFactory,
            IReadColumn readColumn)
        {
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => connectionFactory, connectionFactory);
            Guard.NotNull(() => readColumn, readColumn);

            _prepareQuery = prepareQuery;
            _connectionFactory = connectionFactory;
            _readColumn = readColumn;
        }
        /// <inheritdoc />
        public int Handle(GetErrorRetryCountQuery<T> query)
        {
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetErrorRetryCount);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return _readColumn.ReadAsInt32(CommandStringTypes.GetErrorRetryCount, 0, reader);
                        }
                    }
                }
            }
            return 0;
        }
    }
}
