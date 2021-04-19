// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    /// <inheritdoc />
    public class GetTableExistsTransactionQueryPrepareDecorator : IPrepareQueryHandler<GetTableExistsTransactionQuery, bool>
    {
        private readonly IPrepareQueryHandler<GetTableExistsTransactionQuery, bool> _decorated;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateJobTablesCommandDecorator"/> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        public GetTableExistsTransactionQueryPrepareDecorator(
            IPrepareQueryHandler<GetTableExistsTransactionQuery, bool> decorated)
        {
            Guard.NotNull(() => decorated, decorated);
            _decorated = decorated;
        }

        /// <inheritdoc />
        public void Handle(GetTableExistsTransactionQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            //table name needs to be lower case
            _decorated.Handle(new GetTableExistsTransactionQuery(query.Connection,
                query.Trans, query.TableName.ToLowerInvariant()), dbCommand, commandType );
        }
    }
}
