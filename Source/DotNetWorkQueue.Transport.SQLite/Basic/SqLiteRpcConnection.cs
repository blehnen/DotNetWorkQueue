// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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

namespace DotNetWorkQueue.Transport.SQLite.Basic
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteRpcConnection"/> class.
        /// </summary>
        /// <param name="sendConnection">The send connection.</param>
        /// <param name="sendQueue">The send queue.</param>
        /// <param name="receiveConnection">The receive connection.</param>
        /// <param name="receiveQueue">The receive queue.</param>
        public SqLiteRpcConnection(string sendConnection, string sendQueue, string receiveConnection, string receiveQueue)
        {
            Guard.NotNullOrEmpty(() => sendConnection, sendConnection);
            Guard.NotNullOrEmpty(() => sendQueue, sendQueue);
            Guard.NotNullOrEmpty(() => receiveConnection, receiveConnection);
            Guard.NotNullOrEmpty(() => receiveQueue, receiveQueue);

            _sendConnection = sendConnection;
            _sendQueue = sendQueue;
            _receiveConnection = receiveConnection;
            _receiveQueue = receiveQueue;
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
                    return new SqliteConnectionInformation {ConnectionString = _sendConnection, QueueName = _sendQueue};
                case ConnectionTypes.Receive:
                    return new SqliteConnectionInformation { ConnectionString = _receiveConnection, QueueName = _receiveQueue };
            }
            throw new DotNetWorkQueueException($"unhandled type {connectionType}");
        }
    }
}
