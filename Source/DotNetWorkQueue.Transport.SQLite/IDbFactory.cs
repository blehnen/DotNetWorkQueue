// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
        /// Creates a command for the supplied text, reusing the statements SQLite compiled for it
        /// on this connection where the implementation can.
        /// </summary>
        /// <remarks>
        /// System.Data.SQLite compiles a command's statements on first execution and keeps them on
        /// the command object, so a command created per operation recompiles every time. On the
        /// dequeue script, whose text is long and has several statements, that measured 27,389 ns
        /// and 22,144 B per dequeue against 4,458 ns and 552 B when the command is reused.
        /// Implementations that cannot reuse anything simply return a new command, which is what
        /// the default below does.
        /// </remarks>
        /// <param name="connection">The connection.</param>
        /// <param name="commandText">The command text.</param>
        /// <returns></returns>
        IDbCommand CreateCommand(IDbConnection connection, string commandText)
        {
            var command = CreateCommand(connection);
            command.CommandText = commandText;
            return command;
        }

        /// <summary>
        /// Creates a new instance of <seealso cref="ISQLiteTransactionWrapper"/>
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        ISQLiteTransactionWrapper CreateTransaction(IDbConnection connection);
    }
}
