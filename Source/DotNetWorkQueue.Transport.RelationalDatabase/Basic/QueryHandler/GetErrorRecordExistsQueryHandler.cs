// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Determines if an error record for a message already exists with the specific exception type
    /// </summary>
    internal class GetErrorRecordExistsQueryHandler : IQueryHandler<GetErrorRecordExistsQuery, bool>
    {
        private readonly IPrepareQueryHandler<GetErrorRecordExistsQuery, bool> _prepareQuery;
        private readonly IDbConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetErrorRecordExistsQueryHandler" /> class.
        /// </summary>
        /// <param name="prepareQuery">The prepare query.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        public GetErrorRecordExistsQueryHandler(IPrepareQueryHandler<GetErrorRecordExistsQuery, bool> prepareQuery,
            IDbConnectionFactory connectionFactory)
        {
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => connectionFactory, connectionFactory);

            _prepareQuery = prepareQuery;
            _prepareQuery = prepareQuery;
            _connectionFactory = connectionFactory;
        }
        /// <inheritdoc />
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public bool Handle(GetErrorRecordExistsQuery query)
        {
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetErrorRecordExists);
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }
    }
}
