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

using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// Connection settings for Rpc
    /// </summary>
    public class SqLiteRpcConnection: BaseRpcConnection
    {
        private readonly string _sendConnection;
        private readonly string _sendQueue;
        private readonly string _receiveConnection;
        private readonly string _receiveQueue;
        private readonly IDbDataSource _dataSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteRpcConnection" /> class.
        /// </summary>
        /// <param name="sendConnection">The send connection.</param>
        /// <param name="sendQueue">The send queue.</param>
        /// <param name="receiveConnection">The receive connection.</param>
        /// <param name="receiveQueue">The receive queue.</param>
        /// <param name="dataSource">The data source.</param>
        public SqLiteRpcConnection(string sendConnection, string sendQueue, 
            string receiveConnection, string receiveQueue,
            IDbDataSource dataSource)
        {
            Guard.NotNullOrEmpty(() => sendConnection, sendConnection);
            Guard.NotNullOrEmpty(() => sendQueue, sendQueue);
            Guard.NotNullOrEmpty(() => receiveConnection, receiveConnection);
            Guard.NotNullOrEmpty(() => receiveQueue, receiveQueue);
            Guard.NotNull(() => dataSource, dataSource);

            _sendConnection = sendConnection;
            _sendQueue = sendQueue;
            _receiveConnection = receiveConnection;
            _receiveQueue = receiveQueue;
            _dataSource = dataSource;
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="connectionType">Type of the connection.</param>
        /// <returns></returns>
        public override IConnectionInformation GetConnection(ConnectionTypes connectionType)
        {
            switch (connectionType)
            {
                case ConnectionTypes.NotSpecified:
                case ConnectionTypes.Send:
                    return new SqliteConnectionInformation(_sendQueue, _sendConnection, _dataSource);
                case ConnectionTypes.Receive:
                    return new SqliteConnectionInformation(_receiveQueue, _receiveConnection, _dataSource);
            }
            throw new DotNetWorkQueueException($"unhandled type {connectionType}");
        }
    }
}
