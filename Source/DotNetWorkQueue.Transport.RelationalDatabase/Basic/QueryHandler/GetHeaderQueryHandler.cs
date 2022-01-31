// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <summary>
    /// Obtains a header
    /// </summary>
    public class GetHeaderQueryHandler<T> : IQueryHandler<GetHeaderQuery<T>, IDictionary<string, object>>
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IPrepareQueryHandler<GetHeaderQuery<T>, IDictionary<string, object>> _prepareQuery;
        private readonly IReadColumn _readColumn;
        private readonly ICompositeSerialization _serialization;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetHeaderQueryHandler{T}"/> class.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="prepareQuery">The prepare query.</param>
        /// <param name="readColumn">The read column.</param>
        /// <param name="serialization">The serialization.</param>
        public GetHeaderQueryHandler(IDbConnectionFactory connectionFactory,
            IPrepareQueryHandler<GetHeaderQuery<T>, IDictionary<string, object>> prepareQuery,
            IReadColumn readColumn, ICompositeSerialization serialization)
        {
            Guard.NotNull(() => connectionFactory, connectionFactory);
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => readColumn, readColumn);
            Guard.NotNull(() => serialization, serialization);
            _connectionFactory = connectionFactory;
            _prepareQuery = prepareQuery;
            _readColumn = readColumn;
            _serialization = serialization;
        }
        /// <inheritdoc />
        public IDictionary<string, object> Handle(GetHeaderQuery<T> query)
        {
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetHeader);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) return null;
                        var headerRaw = _readColumn.ReadAsByteArray(CommandStringTypes.GetHeader, 0, reader);
                        if (headerRaw != null)
                        {
                            return _serialization.InternalSerializer
                                .ConvertBytesTo<IDictionary<string, object>>(
                                    headerRaw);
                        }
                    }
                }
            }

            return null;
        }
    }
}
