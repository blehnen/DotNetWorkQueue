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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Connection information for Rpc
    /// </summary>
    public class RedisQueueRpcConnection: BaseRpcConnection
    {
        private readonly string _connection;
        private readonly string _queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueRpcConnection"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="queue">The queue.</param>
        public RedisQueueRpcConnection(string connection, string queue)
        {
            Guard.NotNullOrEmpty(() => connection, connection);
            Guard.NotNullOrEmpty(() => queue, queue);
            _connection = connection;
            _queue = queue;
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
                    return new BaseConnectionInformation(_queue, _connection);
                case ConnectionTypes.Receive:
                    return new BaseConnectionInformation(string.Concat(_queue, "Response"), _connection);
                default:
                    throw new DotNetWorkQueueException($"unhandled type {connectionType}");
            }
        }
    }
}
