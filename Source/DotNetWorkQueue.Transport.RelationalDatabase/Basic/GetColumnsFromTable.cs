// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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

using System;
using System.Collections.Generic;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <inheritdoc />
    public class GetColumnsFromTable: IGetColumnsFromTable
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<GetColumnNamesFromTableQuery, List<string>> _columnQuery;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetColumnsFromTable"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="columnQuery">The column query.</param>
        public GetColumnsFromTable(IConnectionInformation connectionInformation,
            IQueryHandler<GetColumnNamesFromTableQuery, List<string>> columnQuery)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => columnQuery, columnQuery);

            _connectionInformation = connectionInformation;
            _columnQuery = columnQuery;
        }
        /// <inheritdoc />
        public IEnumerable<string> GetColumnsThatAreInBothTables(string table1, string table2)
        {
            var columns1 = _columnQuery.Handle(new GetColumnNamesFromTableQuery(_connectionInformation.ConnectionString, table1));
            var columns2 = _columnQuery.Handle(new GetColumnNamesFromTableQuery(_connectionInformation.ConnectionString, table2));
            return columns1.Intersect(columns2, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
