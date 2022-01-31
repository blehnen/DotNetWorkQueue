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
using System.Data;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <inheritdoc />
    public class ConnectionHeader<TConnection, TTransaction, TCommand> : IConnectionHeader<TConnection, TTransaction, TCommand>
        where TConnection : IDbConnection
        where TTransaction : IDbTransaction
        where TCommand : IDbCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionHeader{TConnection, TTransaction, TCommand}"/> class.
        /// </summary>
        /// <param name="messageContextDataFactory">The message context data factory.</param>
        public ConnectionHeader(IMessageContextDataFactory messageContextDataFactory)
        {
            Guard.NotNull(() => messageContextDataFactory, messageContextDataFactory);
            Connection = messageContextDataFactory.Create<IConnectionHolder<TConnection, TTransaction, TCommand>>("Connection", null);
        }
        /// <inheritdoc />
        public IMessageContextData<IConnectionHolder<TConnection, TTransaction, TCommand>> Connection { get; }
    }
}
