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
using System.Data;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <inheritdoc cref="IDisposable"></inheritdoc>
    /// <summary>
    /// Defines a class that holds onto a connection and transaction. Used to allow holding a transaction open while work is in progress.
    /// </summary>
    /// <typeparam name="TConnection">The type of the connection.</typeparam>
    /// <typeparam name="TTransaction">The type of the transaction.</typeparam>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <seealso cref="System.IDisposable" />
    /// <seealso cref="DotNetWorkQueue.IIsDisposed" />
    public interface IConnectionHolder<TConnection, TTransaction, out TCommand> : IDisposable, IIsDisposed
        where TConnection : IDbConnection
        where TTransaction : IDbTransaction
        where TCommand: IDbCommand
    {
        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        TConnection Connection { get; set; }
        /// <summary>
        /// Gets or sets the transaction.
        /// </summary>
        /// <value>
        /// The transaction.
        /// </value>
        TTransaction Transaction { get; set; }
        /// <summary>
        /// Creates the command.
        /// </summary>
        /// <returns></returns>
        TCommand CreateCommand();
    }
}
