// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Creates a new instance of <see cref="IConnectionHolder{TConnection, TTransaction, TCommand}"/>
    /// </summary>
    /// <typeparam name="TConnection">The type of the connection.</typeparam>
    /// <typeparam name="TTransaction">The type of the transaction.</typeparam>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    public interface IConnectionHolderFactory<TConnection, TTransaction, out TCommand>
        where TConnection : IDbConnection
        where TTransaction : IDbTransaction
        where TCommand : IDbCommand
    {
        /// <summary>
        /// Creates a new instance of <see cref="IConnectionHolder{TConnection, TTransaction, TCommand}"/>
        /// </summary>
        /// <returns></returns>
        IConnectionHolder<TConnection, TTransaction, TCommand> Create();
    }
}
