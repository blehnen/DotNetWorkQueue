﻿// ---------------------------------------------------------------------
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
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Decorator
{
    /// <inheritdoc />
    public class
        GetColumnNamesFromTableQueryPrepareDecorator : IPrepareQueryHandler<GetColumnNamesFromTableQuery, List<string>>
    {
        private readonly IPrepareQueryHandler<GetColumnNamesFromTableQuery, List<string>> _decorated;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateJobTablesCommandDecorator"/> class.
        /// </summary>
        /// <param name="decorated">The decorated.</param>
        public GetColumnNamesFromTableQueryPrepareDecorator(
            IPrepareQueryHandler<GetColumnNamesFromTableQuery, List<string>> decorated)
        {
            Guard.NotNull(() => decorated, decorated);
            _decorated = decorated;
        }

        /// <inheritdoc />
        public void Handle(GetColumnNamesFromTableQuery query, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            //table name needs to be lower case
            _decorated.Handle(new GetColumnNamesFromTableQuery(query.ConnectionString, query.TableName.ToLowerInvariant()), dbCommand,
                commandType);
        }
    }
}
