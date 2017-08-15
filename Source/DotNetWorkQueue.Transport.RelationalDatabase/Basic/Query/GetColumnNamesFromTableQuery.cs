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
using System.Collections.Generic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query
{
    /// <summary>
    /// Returns all of the column names from a table
    /// </summary>
    public class GetColumnNamesFromTableQuery: IQuery<List<string>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetColumnNamesFromTableQuery"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="tableName">Name of the table.</param>
        public GetColumnNamesFromTableQuery(string connectionString, string tableName)
        {
            Guard.NotNullOrEmpty(() => connectionString, connectionString);
            Guard.NotNullOrEmpty(() => tableName, tableName);
            ConnectionString = connectionString;
            TableName = tableName;
        }
        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString { get;}
        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>
        /// The name of the table.
        /// </value>
        public string TableName { get;}
    }
}
