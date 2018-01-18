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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <inheritdoc />
    public class GetTableExistsTransactionQueryHandler: IQueryHandler<GetTableExistsTransactionQuery, bool>
    {
        private readonly IPrepareQueryHandler<GetTableExistsTransactionQuery, bool> _prepareQuery;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetTableExistsQueryHandler" /> class.
        /// </summary>
        /// <param name="prepareQuery">The prepare query.</param>
        public GetTableExistsTransactionQueryHandler(IPrepareQueryHandler<GetTableExistsTransactionQuery, bool> prepareQuery)
        {
            Guard.NotNull(() => prepareQuery, prepareQuery);
            _prepareQuery = prepareQuery;
        }

        /// <inheritdoc />
        public bool Handle(GetTableExistsTransactionQuery query)
        {
            using (var command = query.Connection.CreateCommand())
            {
                command.Transaction = query.Trans;
                _prepareQuery.Handle(query, command, CommandStringTypes.GetTableExists);
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }
    }
}
