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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    internal class GetErrorTrackingUniqueIndexExistsQueryHandler : IQueryHandler<GetErrorTrackingUniqueIndexExistsQuery, bool>
    {
        private readonly IPrepareQueryHandler<GetErrorTrackingUniqueIndexExistsQuery, bool> _prepareQuery;
        private readonly IDbConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetErrorTrackingUniqueIndexExistsQueryHandler"/> class.
        /// </summary>
        /// <param name="prepareQuery">The prepare query.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        public GetErrorTrackingUniqueIndexExistsQueryHandler(
            IPrepareQueryHandler<GetErrorTrackingUniqueIndexExistsQuery, bool> prepareQuery,
            IDbConnectionFactory connectionFactory)
        {
            Guard.NotNull(prepareQuery);
            Guard.NotNull(connectionFactory);

            _prepareQuery = prepareQuery;
            _connectionFactory = connectionFactory;
        }

        /// <inheritdoc />
        public bool Handle(GetErrorTrackingUniqueIndexExistsQuery query)
        {
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetErrorTrackingUniqueIndexExists);
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }
    }
}
