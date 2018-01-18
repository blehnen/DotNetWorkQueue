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
using System.Data;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query
{
    /// <inheritdoc />
    /// <summary>
    /// Determines if a table exists in a schema while part of a transaction
    /// </summary>
    /// <seealso cref="T:System.Boolean" />
    public class GetTableExistsTransactionQuery : IQuery<bool>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetTableExistsQuery" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="trans">The trans.</param>
        /// <param name="tableName">Name of the table.</param>
        public GetTableExistsTransactionQuery(IDbConnection connection, IDbTransaction trans, string tableName)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => trans, trans);
            Guard.NotNullOrEmpty(() => tableName, tableName);

            Connection = connection;
            Trans = trans;
            TableName = tableName;
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        public IDbConnection Connection { get; }
        /// <summary>
        /// Gets the trans.
        /// </summary>
        /// <value>
        /// The trans.
        /// </value>
        public IDbTransaction Trans { get; }
        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>
        /// The name of the table.
        /// </value>
        public string TableName { get; }
    }
}
