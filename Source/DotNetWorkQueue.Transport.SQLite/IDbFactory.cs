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

namespace DotNetWorkQueue.Transport.SQLite
{
    /// <summary>
    /// Creates new db objects
    /// </summary>
    public interface IDbFactory
    {
        /// <summary>
        /// Creates the connection.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="forMemoryHold">if set to <c>true</c> [this connection is our master in-memory connection. This connection keeps the in-memory database alive].</param>
        /// <returns></returns>
        IDbConnection CreateConnection(string connectionString, bool forMemoryHold);

        /// <summary>
        /// Creates the command.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        IDbCommand CreateCommand(IDbConnection connection);

        /// <summary>
        /// Creates a new instance of <seealso cref="ISQLiteTransactionWrapper"/>
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        ISQLiteTransactionWrapper CreateTransaction(IDbConnection connection);
    }
}
