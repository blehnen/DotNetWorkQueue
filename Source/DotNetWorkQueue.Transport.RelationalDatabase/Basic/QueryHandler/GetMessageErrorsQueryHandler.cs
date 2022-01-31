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
    internal class GetMessageErrorsQueryHandler<T> : IQueryHandler<GetMessageErrorsQuery<T>, Dictionary<string, int>>
    {
        private readonly IPrepareQueryHandler<GetMessageErrorsQuery<T>, Dictionary<string, int>> _prepareQuery;
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IReadColumn _readColumn;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetErrorRetryCountQueryHandler{T}" /> class.
        /// </summary>
        /// <param name="prepareQuery">The prepare query.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="readColumn">The read column.</param>
        public GetMessageErrorsQueryHandler(IPrepareQueryHandler<GetMessageErrorsQuery<T>, Dictionary<string, int>> prepareQuery,
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
        public Dictionary<string, int> Handle(GetMessageErrorsQuery<T> query)
        {
            var returnData = new Dictionary<string, int>();
            using (var connection = _connectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetMessageErrors);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var errorType = _readColumn.ReadAsString(CommandStringTypes.GetMessageErrors, 0, reader).Trim();
                            var count = _readColumn.ReadAsInt32(CommandStringTypes.GetMessageErrors, 1, reader);
                            returnData.Add(errorType, count);
                        }
                    }
                }
            }

            return returnData;
        }
    }
}
